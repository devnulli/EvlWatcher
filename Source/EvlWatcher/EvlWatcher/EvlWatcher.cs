using EvlWatcher.Config;
using EvlWatcher.Converter;
using EvlWatcher.Logging;
using EvlWatcher.SystemAPI;
using EvlWatcher.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;

namespace EvlWatcher
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
    public class EvlWatcher : ServiceBase, WCF.IEvlWatcherService
    {
        #region private members

        /// <summary>
        /// this thread does the actual log scanning
        /// </summary>
        private Thread _workerThread;

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
        private static readonly IList<LogTask> _logTasks = new List<LogTask>();

        /// <summary>
        /// adds some extra output
        /// </summary>

        private static List<IPAddress> _lastPolledTempBans = new List<IPAddress>();
        private static List<IPAddress> _lastBannedIPs = new List<IPAddress>();

        private static readonly object _syncObject = new object();

        private volatile bool _stop = false;

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
        public bool GetIsRunning()
        {
            return true;
        }

        public IPAddress[] GetPermanentlyBannedIPs()
        {
            lock (_syncObject)
            {
                return _serviceconfiguration.BlacklistAddresses.ToArray();
            }
        }

        public string[] GetWhiteListEntries()
        {
            lock (_syncObject)
                return _serviceconfiguration.WhitelistPatterns.ToArray();
        }

        public void SetPermanentBan(IPAddress address)
        {
            _serviceconfiguration.AddBlackListAddress(address);

            PushBanList();
        }

        public void ClearPermanentBan(IPAddress address)
        {
            _serviceconfiguration.RemoveBlackListAddress(address);

            PushBanList();
        }

        public void AddWhiteListEntry(string filter)
        {
            _serviceconfiguration.AddWhiteListPattern(filter);

            PushBanList();
        }

        public void RemoveWhiteListEntry(string filter)
        {
            _serviceconfiguration.RemoveWhiteListPattern(filter);

            PushBanList();
        }

        public IPAddress[] GetTemporarilyBannedIPs()
        {
            lock (_syncObject)
            {
                List<IPAddress> result = new List<IPAddress>(_lastPolledTempBans);

                result.RemoveAll(p => _serviceconfiguration.BlacklistAddresses.Contains(p));

                return result.ToArray();
            }
        }

        #endregion

        #region protected operations
        protected override void OnStart(string[] args)
        {
            lock (_syncObject)
            {
                _serviceHost = new ServiceHost(this, new Uri[] { new Uri("net.pipe://localhost") });
                var binding = new NetNamedPipeBinding();
                
                _serviceHost.AddServiceEndpoint(typeof(WCF.IEvlWatcherService), binding, "EvlWatcher");
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
        /// creates generic log tasks from configuration
        /// </summary>
        /// <param name="d"></param>
        private void InitWorkersFromConfig(IQueryable<IPersistentTaskConfiguration> taskConfigurations)
        {
            lock (_syncObject)
            {
                foreach (var config in taskConfigurations.Where(c => c.Active == false))
                    _logger.Dump($"Skipped {config.TaskName} (set inactive)", SeverityLevel.Verbose);

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
                    .ToList();

                FirewallAPI.AdjustIPBanList(banList);

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
                var eventTypesToNewEvents = new Dictionary<string, List<EventRecord>>();
                var eventTypesToTimeFramedEvents = new Dictionary<string, List<EventRecord>>();

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

                    _logger.Dump("Scanning the logs now, scanned logs are:", SeverityLevel.Verbose);

                    DateTime referenceTimeForTimeFramedEvents = DateTime.Now;
                    try
                    {
                        eventTypesToNewEvents.Clear();
                        eventTypesToTimeFramedEvents.Clear();

                        //first read all relevant events (events that are required by any of the tasks)
                        foreach (string requiredEventType in requiredEventTypesToLogTasks.Keys)
                        {
                            eventTypesToNewEvents.Add(requiredEventType, new List<EventRecord>());
                            eventTypesToTimeFramedEvents.Add(requiredEventType, new List<EventRecord>());

                            var eventLogQuery = new EventLogQuery(requiredEventType, PathType.LogName)
                            {
                                ReverseDirection = true
                            };

                            try
                            {
                                var eventLogReader = new EventLogReader(eventLogQuery);
                                EventRecord r;

                                while ((r = eventLogReader.ReadEvent()) != null)
                                {
                                    if (!r.TimeCreated.HasValue)
                                        continue;

                                    bool canbreak = false;

                                    //fill new event list
                                    if (r.TimeCreated > eventTypesToLastEvent[requiredEventType])
                                    {
                                        eventTypesToNewEvents[requiredEventType].Add(r);
                                        eventTypesToLastEvent[requiredEventType] = r.TimeCreated.Value;
                                    }
                                    else
                                        canbreak = true;

                                    //fill time framed event list
                                    if (r.TimeCreated > referenceTimeForTimeFramedEvents.Subtract(new TimeSpan(0, 0, eventTypesToMaxAge[requiredEventType])))
                                        eventTypesToTimeFramedEvents[requiredEventType].Add(r);
                                    else if (canbreak)
                                        break;
                                }
                            }
                            catch (EventLogNotFoundException)
                            {
                                _logger.Dump($"Event Log {requiredEventType} was not found, tasks that require these events will not work", SeverityLevel.Error);
                            }
                        }

                        _logger.Dump($"Scanning finished in {DateTime.Now.Subtract(scanStart).TotalMilliseconds}[ms] ", SeverityLevel.Verbose);


                        //then supply the events to the requesting tasks
                        foreach (string key in requiredEventTypesToLogTasks.Keys)
                        {
                            foreach (LogTask t in requiredEventTypesToLogTasks[key])
                            {
                                if (t.OnlyNew)
                                    t.ProvideEvents(eventTypesToNewEvents[key]);
                                else
                                {
                                    var eventsForThisTask = new List<EventRecord>();
                                    foreach (EventRecord e in eventTypesToTimeFramedEvents[key])
                                    {
                                        if (e.TimeCreated > referenceTimeForTimeFramedEvents.Subtract(new TimeSpan(0, 0, t.EventAge)))
                                            eventsForThisTask.Add(e);
                                    }

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

                        List<IPAddress> blackList = new List<IPAddress>();

                        //let the tasks poll which ips they want to have blocked / or permanently banned
                        foreach (LogTask t in _logTasks)
                        {
                            if (t is IPBlockingLogTask ipTask)
                            {
                                foreach (IPAddress perma in ipTask.GetPermaBanVictims())
                                    SetPermanentBan(perma);

                                List<IPAddress> blockedIPs = ipTask.GetTempBanVictims();


                                _logger.Dump($"Polled {t.Name} and got {blockedIPs.Count} temporary and {_serviceconfiguration.BlacklistAddresses.Count()} permanent ban(s)", SeverityLevel.Verbose);

                                foreach (IPAddress blockedIP in blockedIPs)
                                    if (!blackList.Contains(blockedIP))
                                        blackList.Add(blockedIP);
                            }
                        }

                        _logger.Dump($"\r\n-----Cycle complete, sleeping {_serviceconfiguration.EventLogInterval / 1000} s......\r\n", SeverityLevel.Debug);
                        
                        _lastPolledTempBans = blackList;
                        Random rnd = new Random();
                        int j = rnd.Next(50);
                        for (int i = 0; i < j; i++)
                        {
                            _lastPolledTempBans.Add(new IPAddress(new byte[] { 102, 0, 0, (byte)rnd.Next(200) }));
                        }

                        PushBanList();
                    }
                    catch (Exception executionException)
                    {
                        _logger.Dump(executionException, SeverityLevel.Error);
                    }

                    //wait for next iteration or kill signal
                    try
                    {
                        Thread.Sleep(_serviceconfiguration.EventLogInterval);
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
                            FirewallAPI.ClearIPBanList();
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


        #endregion

        #region public static operations

        public static void Main(string[] args)
        {
            //build dependencies
            ILogger logger = new DefaultLogger();
            IPersistentServiceConfiguration serviceConfiguration = new XmlServiceConfiguration(logger);
            IGenericTaskFactory genericTaskFactory = new DefaultGenericTaskFactory();

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

        #endregion
    }
}
