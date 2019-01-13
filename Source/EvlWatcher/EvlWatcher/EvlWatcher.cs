using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace EvlWatcher
{
    public class EvlWatcher : ServiceBase, WCF.IEvlWatcherService
    {
        #region private members

        Thread _workerThread;

        private ServiceHost _h;
        private static List<LogTask> _logTasks = new List<LogTask>();
        private static bool _runasApplication = false;
        private static bool _verbose = true;
        private static bool _inBan = false;

        private static List<IPAddress> _permaBannedIPs = new List<IPAddress>();
        private static List<string> _whiteListPatterns = new List<string>();
        private static List<IPAddress> _lastPolledTempBans = new List<IPAddress>();
        private static List<IPAddress> _lastBannedIPs = new List<IPAddress>();

        private static object _syncObject = new object();

        private volatile bool _stop = false;

        #endregion

        #region public operations

        public bool GetIsRunning()
        {
            return true;
        }

        public KeyValuePair<DateTime, string>[] GetProtocolSince(DateTime time)
        {
            return new KeyValuePair<DateTime, string>[] { new KeyValuePair<DateTime, string>(DateTime.Now, "Protocol not supported in this version") };
        }

        public IPAddress[] GetPermanentlyBannedIPs()
        {
            lock (_syncObject)
            {
                return _permaBannedIPs.ToArray();
            }
        }

        public string[] GetWhiteListEntries()
        {
            lock (_syncObject)
                return _whiteListPatterns.ToArray();
        }

        public void SetPermanentBan(IPAddress address)
        {
            lock (_syncObject)
            {
                if (!_permaBannedIPs.Contains(address))
                    _permaBannedIPs.Add(address);

                string s = "";
                foreach (IPAddress ip in _permaBannedIPs)
                    s += ip.ToString() + ";";

                WriteConfig("GLOBAL", "Banlist", s);
            }

            DoBan();
        }

        public void ClearPermanentBan(IPAddress address)
        {
            lock (_syncObject)
            {
                if (_permaBannedIPs.Contains(address))
                    _permaBannedIPs.Remove(address);

                string s = "";
                foreach (IPAddress ip in _permaBannedIPs)
                    s += ip.ToString() + ";";

                WriteConfig("GLOBAL", "Banlist", s);
            }

            DoBan();
        }

        public void AddWhiteListEntry(string filter)
        {
            if (filter.Contains(";"))
                return;

            lock (_syncObject)
            {
                if (!_whiteListPatterns.Contains(filter))
                    _whiteListPatterns.Add(filter);

                string s = "";
                foreach (string pattern in _whiteListPatterns)
                    s += pattern + ";";

                WriteConfig("GLOBAL", "WhiteList", s);

            }

            DoBan();
        }

        public void RemoveWhiteListEntry(string filter)
        {
            lock (_syncObject)
            {
                if (_whiteListPatterns.Contains(filter))
                    _whiteListPatterns.Remove(filter);

                string s = "";
                foreach (string pattern in _whiteListPatterns)
                    s += pattern + ";";

                WriteConfig("GLOBAL", "WhiteList", s);
            }

            DoBan();
        }

        public IPAddress[] GetTemporarilyBannedIPs()
        {
            lock (_syncObject)
            {
                List<IPAddress> result = new List<IPAddress>(_lastPolledTempBans);

                foreach (IPAddress a in _permaBannedIPs)
                {
                    if (result.Contains(a))
                        result.Remove(a);
                }

                return result.ToArray();
            }
        }

        #endregion

        #region protected operations

        protected override void OnStart(string[] args)
        {
            lock (_syncObject)
            {
                _h = new ServiceHost(typeof(EvlWatcher), new Uri[] { new Uri("net.pipe://localhost") });
                _h.AddServiceEndpoint(typeof(WCF.IEvlWatcherService), new NetNamedPipeBinding(), "EvlWatcher");
                _h.Open();

                _workerThread = new Thread(new ThreadStart(this.Run));
                _workerThread.IsBackground = true;
                _workerThread.Start();
            }
        }

        protected override void OnStop()
        {
            _h.Close();

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
                    Dump("Service could not terminate normally.", EventLogEntryType.Warning);
                    return;
                }
            }

            Dump("Service terminated OK", EventLogEntryType.Information);
        }

        #endregion

        #region private operations

        private string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }

        private void WriteConfig(string task, string property, string value)
        {
            if (_verbose)
                Dump($"Writing config for: {task}: {property} = {value}", EventLogEntryType.Information);

            XDocument d = XDocument.Load(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config.xml"));
            XElement taskEl = d.Root.Element(task);
            if (taskEl == null)
            {
                taskEl = new XElement(task);
                d.Root.Add(taskEl);
            }
            if (taskEl != null)
            {
                XElement val = taskEl.Element(property);
                if (val == null)
                {
                    val = new XElement(property);
                    taskEl.Add(val);
                }
                if (val != null)
                {
                    val.Value = value.ToString();
                }

            }
            d.Save(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config.xml"));
        }

        private void WriteConfig(string task, string property, int value)
        {
            WriteConfig(task, property, value.ToString());
        }

        private void InitExternalWorkerDLLs(XDocument d)
        {
            lock (_syncObject)
            {
                string loadedTasks = "";
                string failedTasks = "";

                //do startup
                foreach (FileInfo fileInfo in new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.GetFiles())
                {
                    if (!fileInfo.FullName.EndsWith(".dll"))
                        continue;

                    Assembly a = null;
                    try
                    {
                        a = Assembly.LoadFrom(fileInfo.FullName);
                    }
                    catch
                    {
                        Dump($"Could not load assembly {fileInfo.FullName}", EventLogEntryType.Warning);
                        continue;
                    }

                    foreach (Type t in a.GetTypes())
                    {
                        if (!t.IsAbstract && t.IsSubclassOf(typeof(LogTask)))
                        {
                            try
                            {
                                LogTask instance = (LogTask)Activator.CreateInstance(t);

                                loadedTasks += $"\n{instance.Name}\n{instance.Description}\n";

                                _logTasks.Add(instance);
                            }
                            catch (Exception e)
                            {
                                failedTasks += $"\n{t.Name}\n Reason: {e.Message}";
                            }
                        }
                    }
                }
                if (loadedTasks.Length > 0 || failedTasks.Length > 0)
                    Dump($"External DLLs loaded, loaded tasks are: \n{loadedTasks}" + (failedTasks.Length > 0 ? $"\nFailing Tasks:\n{failedTasks}" : ""), failedTasks.Length > 0 ? EventLogEntryType.Warning : EventLogEntryType.Information);
                else
                    Dump("No external DLLs loaded", EventLogEntryType.Information);
            }
        }

        private void InitWorkersFromConfig(XDocument d)
        {
            lock (_syncObject)
            {
                string loadedTasks = "";
                string failedTasks = "";

                try
                {
                    HashSet<string> taskNames = new HashSet<string>();
                    foreach (string taskToLoad in from e in d.Root.Element("GenericTaskLoader").Elements("Load") select e.Value)
                    {
                        taskNames.Add(taskToLoad);
                    }
                    foreach (string s in taskNames)
                    {
                        var e = d.Root.Element(s);
                        try
                        {
                            LogTask instance = GenericTask.FromXML(e);

                            loadedTasks += $"\n{instance.Name}\n{ instance.Description}\n";

                            _logTasks.Add(instance);
                        }
                        catch (Exception ex)
                        {
                            failedTasks += $"\n{s}\n Reason: {ex.Message}";
                        }
                    }
                }
                catch
                {
                    Dump("Did not load default tasks. None present, or the XML is corrupted", EventLogEntryType.Warning);
                    throw;
                }

                if (loadedTasks.Length > 0 || failedTasks.Length > 0)
                    Dump($"Generic Tasks loaded, loaded tasks are: \n{loadedTasks}" + (failedTasks.Length > 0 ? $"\nFailing Tasks:\n{failedTasks}" : ""), failedTasks.Length > 0 ? EventLogEntryType.Warning : EventLogEntryType.Information);
                else
                    Dump("No Generic Tasks loaded", EventLogEntryType.Information);
            }
        }

        private void DoBan()
        {
            lock (_syncObject)
            {
                if (_inBan == true)
                    return;

                try
                {
                    _inBan = true;

                    List<IPAddress> banList = new List<IPAddress>();
                    if (_lastPolledTempBans != null)
                    {
                        foreach (IPAddress a in _lastPolledTempBans)
                            if (!banList.Contains(a))
                                banList.Add(a);
                    }
                    if (_permaBannedIPs != null)
                    {
                        foreach (IPAddress p in _permaBannedIPs)
                            if (!banList.Contains(p))
                                banList.Add(p);
                    }

                    List<IPAddress> unbanned = new List<IPAddress>();
                    foreach (IPAddress i in banList)
                    {
                        foreach (string pattern in _whiteListPatterns)
                        {
                            if (IsPatternMatch(i, pattern) && !unbanned.Contains(i))
                                unbanned.Add(i);
                        }
                    }

                    foreach (IPAddress u in unbanned)
                        banList.Remove(u);

                    FirewallAPI.AdjustIPBanList(banList);

                    foreach (IPAddress ip in _lastBannedIPs)
                    {
                        if (!banList.Contains(ip))
                        {
                            Dump($"Removed {ip.ToString()} from the ban list", EventLogEntryType.Information);
                        }
                    }

                    foreach (IPAddress ip in banList)
                    {
                        if (!_lastBannedIPs.Contains(ip))
                        {
                            Dump($"Banned {ip.ToString()}", EventLogEntryType.Information);
                        }
                    }

                    _lastBannedIPs = banList;
                }
                finally
                {
                    _inBan = false;
                }
            }
        }

        private bool IsPatternMatch(IPAddress i, string pattern)
        {
            string s = i.ToString();
            string p = WildcardToRegex(pattern);
            Regex regex = new Regex(p);
            return regex.IsMatch(s);
        }

        private void Run()
        {
            try
            {
                //Init Worker Thread
                LoadConfiguration();

                //prepare datastructures
                Dictionary<string, List<LogTask>> requiredEventTypesToLogTasks = new Dictionary<string, List<LogTask>>();
                foreach(LogTask l in _logTasks)
                {
                    foreach(string s in l.EventPath)
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

                    if (_verbose)
                        Dump("Scanning the logs now.", EventLogEntryType.Information, true);

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

                            var eventLogQuery = new EventLogQuery(requiredEventType, PathType.LogName);
                            eventLogQuery.ReverseDirection = true;
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
                            catch(EventLogNotFoundException)
                            {
                                Dump($"Event Log {requiredEventType} was not found, tasks that require these events will not work", EventLogEntryType.Warning);
                            }
                        }

                        if (_verbose)
                        {
                            Dump($"Scanning finished in {DateTime.Now.Subtract(scanStart).TotalMilliseconds}[ms] ", EventLogEntryType.Information, true);
                        }

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

                                    if (_verbose)
                                    {
                                        Dump($"Provided {eventsForThisTask.Count} events for {t.Name}", EventLogEntryType.Information, true);
                                    }
                                    if (eventsForThisTask.Count > 0)
                                    {
                                        DateTime start = DateTime.Now;

                                        t.ProvideEvents(eventsForThisTask);

                                        if (DateTime.Now.Subtract(start).TotalMilliseconds > 500)
                                            Dump($"Warning: Task {t.Name} takes a lot of resources. This can make your server vulnerable to DOS attacks. Try better boosters.", EventLogEntryType.Warning);
                                    }
                                }
                            }
                        }

                        List<IPAddress> blackList = new List<IPAddress>();

                        //let the tasks poll which ips they want to have blocked / or permanently banned
                        foreach (LogTask t in _logTasks)
                        {
                            if (t is IPBlockingLogTask)
                            {
                                foreach (IPAddress perma in ((IPBlockingLogTask)t).GetPermaBanVictims())
                                    SetPermanentBan(perma);

                                List<IPAddress> blockedIPs = ((IPBlockingLogTask)t).GetTempBanVictims();

                                if (_verbose)
                                    Dump($"Polled {t.Name} and got {blockedIPs.Count} temporary and {_permaBannedIPs.Count} permanent ban(s)", EventLogEntryType.Information, true);

                                foreach (IPAddress blockedIP in blockedIPs)
                                    if (!blackList.Contains(blockedIP))
                                        blackList.Add(blockedIP);
                            }
                        }

                        _lastPolledTempBans = blackList;
                        DoBan();
                    }
                    catch (Exception executionException)
                    {
                        Dump(executionException, EventLogEntryType.Error);
                    }

                    //wait for next iteration or kill signal
                    try
                    {
                        Thread.Sleep(Constants.ThreadSleep);
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
                            Dump(ex, EventLogEntryType.Warning);
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Dump(e, EventLogEntryType.Error);
                Stop();
            }
        }

        private void LoadConfiguration()
        {
            XDocument d = XDocument.Load(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config.xml"));

            LoadGlobalSettings(d);
            InitWorkersFromConfig(d);
            InitExternalWorkerDLLs(d);
        }

        private void LoadGlobalSettings(XDocument d)
        {
            XElement debugModeElement = d.Root.Element("DebugMode");
            if (debugModeElement != null)
            {
                _verbose = bool.Parse(debugModeElement.Value);
            }

            XElement globalConfig = d.Root.Element("GLOBAL");
            if (globalConfig != null)
            {
                XElement banlist = globalConfig.Element("Banlist");
                if (banlist != null)
                {
                    string banstring = banlist.Value;
                    foreach (string ip in banstring.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _permaBannedIPs.Add(IPAddress.Parse(ip));

                    }
                    if (_verbose)
                        Dump($"Loaded permabanlist: {banstring}", EventLogEntryType.Information);
                }

                XElement whitelist = globalConfig.Element("WhiteList");
                if (whitelist != null)
                {
                    string wstring = whitelist.Value;
                    foreach (string ipPattern in wstring.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _whiteListPatterns.Add(ipPattern);

                    }
                    if (_verbose)
                        Dump($"Loaded whitelist: {wstring}", EventLogEntryType.Information);
                }
            }
        }

        #endregion

        #region public static operations

        public static void Dump(Exception e, EventLogEntryType t)
        {
            Dump(e.Message, t);
        }

        public static void Dump(string s, EventLogEntryType t)
        {
            Dump(s, t, false);
        }

        public static void Dump(string s, EventLogEntryType t, bool supressProtocol)
        {
            string source = "EvlWatcher";
            string log = "Application";

            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, log);

            EventLog.WriteEntry(source, s, t);
            Console.WriteLine(s);
        }

        public static void Main(string[] args)
        {
            if (!_runasApplication)
            {
                //service
                Run(new EvlWatcher());
            }
            else
            {
                //debug
                EvlWatcher w = new EvlWatcher();
                w.OnStart(null);
                Thread.Sleep(60000000);
                w.OnStop();
            }
        }

        #endregion
    }
}
