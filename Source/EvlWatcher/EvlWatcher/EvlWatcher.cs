using EvlWatcher.Config;
using EvlWatcher.Converter;
using EvlWatcher.DTOs;
using EvlWatcher.Logging;
using EvlWatcher.SystemAPI;
using EvlWatcher.Tasks;
using EvlWatcher.WCF;
using EvlWatcher.WCF.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;

namespace EvlWatcher
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
    public class EvlWatcher : ServiceBase, IEvlWatcherService
    {
        #region private members

        /// <summary>
        /// this thread does the actual log scanning
        /// </summary>
        private Thread _workerThread;
        private bool _disposed = false;

        private readonly FirewallAPI _firewallApi = new FirewallAPI();

        private readonly ILogger _logger;
        private readonly IPersistentServiceConfiguration _serviceconfiguration;
        private readonly IGenericTaskFactory _genericTaskFactory;

        /// <summary>
        /// this is the servicehost for management apps
        /// </summary>
        private ServiceHost _serviceHost;

        /// <summary>
        /// all loaded tasks
        /// </summary>
        private static readonly List<LogTask> _logTasks = new List<LogTask>();

        /// <summary>
        /// adds some extra output
        /// </summary>

        private static List<IPAddress> _lastPolledTempBans = new List<IPAddress>();
        private static List<IPAddress> _lastBannedIPs = new List<IPAddress>();

        private static readonly object _syncObject = new object();

        private volatile bool _stop = false;

        private bool IsClientAdministrator()
        {
            if( ServiceSecurityContext.Current.WindowsIdentity.IsAuthenticated &&
                new WindowsPrincipal(ServiceSecurityContext.Current.WindowsIdentity).IsInRole(WindowsBuiltInRole.Administrator) )
            {
                return true;
            }

            return false;
        }

        #endregion

        #region public constructor

        public EvlWatcher(ILogger logger, IPersistentServiceConfiguration configuration, IGenericTaskFactory genericTaskFactory)
        {
            _logger = logger;
            _serviceconfiguration = configuration;
            _genericTaskFactory = genericTaskFactory;
        }

        #endregion

        #region public operations

        public GlobalConfigDTO GetGlobalConfig()
        {
            EnsureClientPrivileges();

            return new GlobalConfigDTO()
            {
                ConsoleBackLog = _serviceconfiguration.ConsoleBackLog,
                EventLogInterval = _serviceconfiguration.EventLogInterval,
                LogLevel = (SeverityLevelDTO)Enum.Parse(typeof(SeverityLevelDTO), _serviceconfiguration.LogLevel.ToString()),
                GenericTaskConfigurations = _logTasks.Where(t => t is GenericIPBlockingTask).Select(t => (GenericIPBlockingTask)t).Select(ipt => new GenericIPBlockingTaskDTO() {
                    Active = ipt.Active,
                    Description = ipt.Description,
                    EventAge = ipt.EventAge,
                    EventPath = ipt.EventPath,
                    LockTime = ipt.LockTime,
                    OnlyNewEvents = ipt.OnlyNew,
                    PermaBanCount = ipt.PermaBanCount,
                    Regex = ipt.Regex.ToString(),
                    RegexBoosters = ipt.Boosters,
                    TaskName = ipt.Name,
                    TriggerCount = ipt.TriggerCount

                }).ToList()

            };
        }
        public bool GetIsRunning()
        {
            EnsureClientPrivileges();

            return true;
        }

        public IPAddress[] GetPermanentlyBannedIPs()
        {
            EnsureClientPrivileges();

            lock (_syncObject)
            {
                return _serviceconfiguration.BlacklistAddresses.ToArray();
            }
        }

        public string[] GetWhiteListEntries()
        {
            EnsureClientPrivileges();

            lock (_syncObject)
                return _serviceconfiguration.WhitelistPatterns.ToArray();
        }

        /// <summary>
        /// WCF
        /// </summary>
        /// <param name="address"></param>
        public void SetPermanentBan(IPAddress address)
        {
            EnsureClientPrivileges();

            SetPermanentBanInternal(new IPAddress[] { address });
        }

        public void ClearPermanentBan(IPAddress address)
        {
            EnsureClientPrivileges();

            _serviceconfiguration.RemoveBlackListAddress(address);

            PushBanList();
        }

        public void AddWhiteListEntry(string filter)
        {
            EnsureClientPrivileges();

            _serviceconfiguration.AddWhiteListPattern(filter);

            PushBanList();
        }

        public void RemoveWhiteListEntry(string filter)
        {
            EnsureClientPrivileges();

            _serviceconfiguration.RemoveWhiteListPattern(filter);

            PushBanList();
        }

        public IPAddress[] GetTemporarilyBannedIPs()
        {
            EnsureClientPrivileges();

            lock (_syncObject)
            {
                List<IPAddress> result = new List<IPAddress>(_lastPolledTempBans);

                result.RemoveAll(p => _serviceconfiguration.BlacklistAddresses.Contains(p));
                result.RemoveAll(p => IsWhiteListed(p));

                return result.ToArray();
            }
        }

        public IList<LogEntryDTO> GetConsoleHistory()
        {
            EnsureClientPrivileges();

            lock (_syncObject)
            {
                return _logger.GetConsoleHistory().Select(entry => new LogEntryDTO() { 
                    Date = entry.Date, 
                    Message = entry.Message, 
                    Severity = (SeverityLevelDTO)Enum.Parse(typeof(SeverityLevelDTO), entry.Severity.ToString()) }).ToList();
            }
        }

        #endregion

        #region protected operations

        protected override void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }

            if (disposing)
            {
                
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        protected override void OnStart(string[] args)
        {
            lock (_syncObject)
            {
                _serviceHost = new ServiceHost(this, new Uri[] { new Uri("net.pipe://localhost") });
                var binding = new NetNamedPipeBinding();
                
                _serviceHost.AddServiceEndpoint(typeof(IEvlWatcherService), binding, "EvlWatcher");
                _serviceHost.Open();

                _workerThread = new Thread(new ThreadStart(Run))
                {
                    IsBackground = true
                };
                _workerThread.Start();
            }
        }

        protected override void OnStop()
        {
            _serviceHost.Close();

            //signal the thread to stop
            lock (_syncObject)
            {
                _stop = true;
            }
            _workerThread.Interrupt();

            DateTime start = DateTime.Now;

            //wait for the thread to come back
            while (_workerThread.IsAlive)
            {
                try
                {
                    Thread.Sleep(100);
                }
                catch
                {
                }
                //if the thread doesnt come back in 5s post error and exit hard
                if (DateTime.Now.Subtract(start).TotalSeconds > 5)
                {
                    _logger.Dump("Service could not terminate normally.", SeverityLevel.Warning);
                    return;
                }
            }

            _logger.Dump("Service terminated OK", SeverityLevel.Info);
        }

        #endregion

        #region private operations

        /// <summary>
        /// ensures the requestor is authenticated as privileged user
        /// </summary>
        private void EnsureClientPrivileges()
        {
            if (!IsClientAdministrator())
            {
                _logger.Dump($"There was an attempt to access the named pipe without authorization. ({ServiceSecurityContext.Current.WindowsIdentity.Name})", SeverityLevel.Warning);
                throw new FaultException<ServiceFaultDTO>(
                    new ServiceFaultDTO(
                        ServiceErorCode.clientNotAdministrator,
                        $"Your account {ServiceSecurityContext.Current.WindowsIdentity.Name} is not an Administrator! Please run this software with Administrator privileges. The client will exit..."
                        , true), "error"
                    );
            }
        }

        /// <summary>
        /// creates generic log tasks from configuration
        /// </summary>
        private void InitWorkersFromConfig(IQueryable<IPersistentTaskConfiguration> taskConfigurations)
        {
            lock (_syncObject)
            {
                foreach (var config in taskConfigurations.Where(c => c.Active == false))
                    _logger.Dump($"Skipped {config.TaskName} (set inactive)", SeverityLevel.Info);

                foreach (var config in taskConfigurations.Where(c => c.Active == true))
                {
                    _logTasks.Add(_genericTaskFactory.CreateFromConfiguration(config));
                }
            }
        }

        /// <summary>
        /// returns true when given address is whitelisted and should not be banned
        /// </summary>
        private bool IsWhiteListed(IPAddress address)
        {
            return _serviceconfiguration.WhitelistPatterns
                .Any(p => IsPatternMatch(address, p));
        }

        /// <summary>
        /// Pushes the current ban list down into the system API
        /// </summary>
        private void PushBanList()
        {
            lock (_syncObject)
            {
                List<IPAddress> banList = _lastPolledTempBans
                    .Union(_serviceconfiguration.BlacklistAddresses)
                    .Distinct()
                    .Where(address => !IsWhiteListed(address))
                    .Where(address => !address.Equals(IPAddress.Any))
                    .ToList();

                _firewallApi.AdjustIPBanList(banList);

                foreach (IPAddress ip in _lastBannedIPs.Where(ip => !banList.Contains(ip)))
                    _logger.Dump($"Removed {ip} from the ban list", SeverityLevel.Info);

                foreach (IPAddress ip in banList.Where(ip => !_lastBannedIPs.Contains(ip)))
                    _logger.Dump($"Banned {ip}", SeverityLevel.Info);

                _lastBannedIPs = banList;
                _logger.Dump($"Pushed {banList.Count} IPs down to the firewall for banning.", SeverityLevel.Debug);
            }
        }

        private bool IsPatternMatch(IPAddress i, string pattern)
        {
            string s = i.ToString();
            string p = WildcardToRegexConverter.WildcardToRegex(pattern);
            Regex regex = new Regex(p);
            return regex.IsMatch(s);
        }

        private void Run()
        {
            //reload configuration in case of external changes
            _serviceconfiguration.Load();

            //create generic tasks from configuration
            InitWorkersFromConfig(_serviceconfiguration.TaskConfigurations);

            try
            {
                //prepare datastructures
                Dictionary<string, List<LogTask>> requiredEventTypesToLogTasks = new Dictionary<string, List<LogTask>>();
                foreach (LogTask l in _logTasks)
                {
                    foreach (string s in l.EventPath)
                    {
                        if (!requiredEventTypesToLogTasks.ContainsKey(s))
                            requiredEventTypesToLogTasks[s] = new List<LogTask>();

                        requiredEventTypesToLogTasks[s].Add(l);
                    }
                }

                var eventTypesToLastEvent = new Dictionary<string, DateTime>();
                var eventTypesToMaxAge = new Dictionary<string, int>();
                var eventTypesToNewEvents = new Dictionary<string, List<ExtractedEventRecord>>();
                var eventTypesToTimeFramedEvents = new Dictionary<string, List<ExtractedEventRecord>>();

                //load structure so that only required events are read
                foreach (string requiredEventType in requiredEventTypesToLogTasks.Keys)
                {
                    eventTypesToLastEvent.Add(requiredEventType, DateTime.Now);
                    eventTypesToMaxAge.Add(requiredEventType, 0);
                    foreach (LogTask t in requiredEventTypesToLogTasks[requiredEventType])
                    {
                        if (!t.OnlyNew && eventTypesToMaxAge[requiredEventType] < t.EventAge)
                            eventTypesToMaxAge[requiredEventType] = t.EventAge;
                    }
                }

                //start monitoring the logs
                while (true)
                {
                    DateTime scanStart = DateTime.Now;

                    _logger.Dump($"Scanning the logs now.", SeverityLevel.Debug);
                    

                    DateTime referenceTimeForTimeFramedEvents = DateTime.Now;
                    try
                    {
                        eventTypesToNewEvents.Clear();
                        eventTypesToTimeFramedEvents.Clear();

                        //first read all relevant events (events that are required by any of the tasks)
                        foreach (string requiredEventType in requiredEventTypesToLogTasks.Keys.ToList())
                        {
                            _logger.Dump($"Scanning {requiredEventType}", SeverityLevel.Debug);
                            eventTypesToNewEvents.Add(requiredEventType, new List<ExtractedEventRecord>());
                            eventTypesToTimeFramedEvents.Add(requiredEventType, new List<ExtractedEventRecord>());

                            var eventLogQuery = new EventLogQuery(requiredEventType, PathType.LogName)
                            {
                                ReverseDirection = true
                            };

                            try
                            {
                                //if you crash here, you are not admin, try to restart as admin
                                using (var eventLogReader = new EventLogReader(eventLogQuery))
                                {
                                    EventRecord r;

                                    while ((r = eventLogReader.ReadEvent()) != null)
                                    {
                                        //r.Dispose();
                                        if (!r.TimeCreated.HasValue)
                                            continue;

                                        ExtractedEventRecord eer = new ExtractedEventRecord()
                                        {
                                            TimeCreated = r.TimeCreated.Value,
                                            Xml = r.ToXml()
                                        };

                                        r.Dispose();

                                        bool canbreak = false;

                                        //fill new event list
                                        if (r.TimeCreated > eventTypesToLastEvent[requiredEventType])
                                        {
                                            eventTypesToNewEvents[requiredEventType].Add(eer);
                                            eventTypesToLastEvent[requiredEventType] = r.TimeCreated.Value;
                                        }
                                        else
                                            canbreak = true;

                                        //fill time framed event list
                                        if (r.TimeCreated > referenceTimeForTimeFramedEvents.Subtract(new TimeSpan(0, 0, eventTypesToMaxAge[requiredEventType])))
                                            eventTypesToTimeFramedEvents[requiredEventType].Add(eer);
                                        else if (canbreak)
                                            break;
                                    }
                                }
                            }
                            catch (EventLogNotFoundException)
                            {
                                _logger.Dump($"Event Log {requiredEventType} was not found, tasks that require these events will not work and are disabled.", SeverityLevel.Info);
                                _logTasks.RemoveAll(l => l.EventPath.Contains(requiredEventType));
                                requiredEventTypesToLogTasks.Remove(requiredEventType);

                            }
                        }

                        _logger.Dump($"Scanning finished in {DateTime.Now.Subtract(scanStart).TotalMilliseconds}[ms] ", SeverityLevel.Debug);

                        //then supply the events to the requesting tasks
                        foreach (string key in requiredEventTypesToLogTasks.Keys)
                        {
                            foreach (LogTask t in requiredEventTypesToLogTasks[key])
                            {
                                if (t.OnlyNew)
                                    t.ProvideEvents(eventTypesToNewEvents[key]);
                                else
                                {
                                    var eventsForThisTask = new List<ExtractedEventRecord>();
                                    foreach (ExtractedEventRecord e in eventTypesToTimeFramedEvents[key])
                                    {
                                        if (e.TimeCreated > referenceTimeForTimeFramedEvents.Subtract(new TimeSpan(0, 0, t.EventAge)))
                                            eventsForThisTask.Add(e);
                                    }

                                    if (eventsForThisTask.Count > 0)
                                        _logger.Dump($"Provided {eventsForThisTask.Count} events for {t.Name}", SeverityLevel.Verbose);

                                    if (eventsForThisTask.Count > 0)
                                    {
                                        DateTime start = DateTime.Now;

                                        t.ProvideEvents(eventsForThisTask);

                                        if (DateTime.Now.Subtract(start).TotalMilliseconds > 500)
                                            _logger.Dump($"Warning: Task {t.Name} takes a lot of resources. This can make your server vulnerable to DOS attacks. Try better boosters.", SeverityLevel.Warning);
                                    }
                                }
                            }
                        }

                        List<IPAddress> polledTempBansOfThisCycle = new List<IPAddress>();
                        List<IPAddress> polledPermaBansOfThisCycle = new List<IPAddress>();

                        //let the tasks poll which ips they want to have blocked / or permanently banned
                        foreach (LogTask t in _logTasks)
                        {
                            if (t is IPBlockingLogTask ipTask)
                            {
                                List<IPAddress> polledTempBansOfThisTask = ipTask.GetTempBanVictims();
                                List<IPAddress> polledPermaBansOfThisTask = ipTask.GetPermaBanVictims();

                                _logger.Dump($"Polled {t.Name} and got {polledTempBansOfThisTask.Count} temporary and {polledPermaBansOfThisTask.Count()} permanent ban(s)", SeverityLevel.Verbose);

                                polledPermaBansOfThisCycle.AddRange(polledPermaBansOfThisTask.Where(ip => !polledPermaBansOfThisCycle.Contains(ip)).ToList());
                                polledTempBansOfThisCycle.AddRange(polledTempBansOfThisTask.Where(ip => !polledTempBansOfThisCycle.Contains(ip)).ToList());
                            }
                        }

                        _logger.Dump($"\r\n-----Cycle complete, sleeping {_serviceconfiguration.EventLogInterval} s......\r\n", SeverityLevel.Debug);

                        SetPermanentBanInternal(polledPermaBansOfThisCycle.ToArray(), pushBanList: false);
                        _lastPolledTempBans = polledTempBansOfThisCycle;
                        
                        PushBanList();
                    }
                    catch (Exception executionException)
                    {
                        _logger.Dump(executionException, SeverityLevel.Error);
                    }

                    //wait for next iteration or kill signal
                    try
                    {
                        Thread.Sleep(_serviceconfiguration.EventLogInterval * 1000);
                    }
                    catch (ThreadInterruptedException)
                    {

                    }

                    //check if need to terminate
                    bool terminate;
                    lock (_syncObject)
                    {
                        terminate = _stop;
                    }
                    if (terminate)
                    {
                        try
                        {
                            _firewallApi.ClearIPBanList();
                        }
                        catch (Exception ex)
                        {
                            _logger.Dump(ex, SeverityLevel.Warning);
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Dump(e, SeverityLevel.Error);
                Stop();
            }
        }

        private void SetPermanentBanInternal(IPAddress[] addressList, bool pushBanList=true)
        {
            foreach (IPAddress address in addressList)
                _serviceconfiguration.AddBlackListAddress(address);

            if (pushBanList)
                PushBanList();
        }


        #endregion

        #region public static operations

        public static void Main()
        {
            //build dependencies
            ILogger logger = new DefaultLogger();
            IPersistentServiceConfiguration serviceConfiguration = new XmlServiceConfiguration(logger);
            IGenericTaskFactory genericTaskFactory = new DefaultGenericTaskFactory(logger);

            if (!Environment.UserInteractive)
            {
                Run(new EvlWatcher(logger, serviceConfiguration, genericTaskFactory));
            }
            else
            {
                //debug
                EvlWatcher w = new EvlWatcher(logger, serviceConfiguration, genericTaskFactory);
                w.OnStart(null);
                Thread.Sleep(60000000);
                w.OnStop();
            }
        }

        public void SaveGlobalConfig(SeverityLevelDTO logLevel, int consoleBackLog, int checkInterval)
        {
            _serviceconfiguration.LogLevel = (SeverityLevel) Enum.Parse(typeof(SeverityLevel), logLevel.ToString());
            _serviceconfiguration.ConsoleBackLog = consoleBackLog;
            _serviceconfiguration.EventLogInterval = checkInterval;
        }

        public void RemoveTemporaryBan(IPAddress address)
        {
            EnsureClientPrivileges();

            lock (_syncObject)
            {
                _logger.Dump($"Removing IP {address} from temporary ban list", SeverityLevel.Info);
                foreach (var ipBlockingTask in _logTasks.Where(t => t is IPBlockingLogTask).Select(t => t as IPBlockingLogTask))
                {
                    ipBlockingTask.Forget(address);
                }
                _lastPolledTempBans.Remove(address);
                PushBanList();
            }
        }

        public void SetPermanentBans(IPAddress[] addressList)
        {
            EnsureClientPrivileges();

            SetPermanentBanInternal(addressList);
        }

        #endregion
    }
}
