using EvlWatcher.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Linq;



namespace EvlWatcher.Config
{
    internal class XmlServiceConfiguration : IPersistentServiceConfiguration
    {
        #region public constructor

        public XmlServiceConfiguration(ILogger logger)
        {
            _logger = logger;
            LoadConfiguration();
        }

        #endregion

        #region public properties
        public bool DebugModeEnabled { get; set; }
        public int EventLogInterval { get; set; }

        public IQueryable<string> WhitelistPatterns => _whiteListPatterns.AsQueryable();

        public IQueryable<IPAddress> BlacklistAddresses => _blacklistAddresses.AsQueryable();

        public IQueryable<IPersistentTaskConfiguration> TaskConfigurations
        {
            get
            {
                return _taskConfigurations.AsQueryable();
            }
        }

        public SeverityLevel ConsoleLevel
        {
            get
            {
                return _logger.ConsoleLevel;
            }
            set
            {
                if (_logger.ConsoleLevel != value)
                {
                    _logger.ConsoleLevel = value;
                    if (!_inLoading)
                        WriteConfig("Global", "ConsoleLevel", value.ToString());
                }
            }
        }

        public SeverityLevel LogLevel
        {
            get
            {
                return _logger.LogLevel;
            }
            set
            {
                if (_logger.LogLevel != value)
                {
                    _logger.LogLevel = value;
                    if (!_inLoading)
                        WriteConfig("Global", "LogLevel", value.ToString());
                }
            }
        }

        #endregion

        #region public operations
        public bool AddWhiteListPattern(string pattern)
        {
            bool changed = false;

            if (pattern.Contains(";"))
                return changed;

            lock (_syncObject)
            {
                if (!_whiteListPatterns.Contains(pattern))
                {
                    _whiteListPatterns.Add(pattern);
                    changed = true;

                    string s = "";
                    foreach (string p in _whiteListPatterns)
                        s += p + ";";

                    WriteConfig("Global", "WhiteList", s);
                }
            }

            return changed;
        }

        public bool RemoveWhiteListPattern(string pattern)
        {
            bool changed = false;

            lock (_syncObject)
            {
                if (_whiteListPatterns.Contains(pattern))
                {
                    _whiteListPatterns.Remove(pattern);
                    changed = true;

                    string s = "";
                    foreach (string p in _whiteListPatterns)
                        s += p + ";";

                    WriteConfig("Global", "WhiteList", s);
                }
            }

            return changed;
        }

        public bool AddBlackListAddress(IPAddress address)
        {
            bool changed = false;

            lock (_syncObject)
            {
                if (!_blacklistAddresses.Contains(address))
                {
                    _blacklistAddresses.Add(address);
                    changed = true;

                    string s = "";
                    foreach (IPAddress ip in _blacklistAddresses)
                        s += ip.ToString() + ";";

                    WriteConfig("GLOBAL", "Banlist", s);
                }
            }

            return changed;
        }

        public bool RemoveBlackListAddress(IPAddress address)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region private members

        private readonly object _syncObject = new object();

        private readonly IList<IPAddress> _blacklistAddresses = new List<IPAddress>();
        private readonly IList<string> _whiteListPatterns = new List<string>();
        private readonly IList<IPersistentTaskConfiguration> _taskConfigurations = new List<IPersistentTaskConfiguration>();

        private readonly ILogger _logger;
        private bool _inLoading = false;

        #endregion

        #region private operations

        private void WriteConfig(string task, string property, string value)
        {
            _logger.Dump($"Writing config for: {task}: {property} = {value}", SeverityLevel.Verbose);

            XDocument d = XDocument.Load(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config\\config.xml"));
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
            d.Save(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config\\config.xml"));
        }

        private void LoadConfiguration()
        {
            try
            {
                _inLoading = true;
                XDocument d = XDocument.Load(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config\\config.xml"));

                LoadGlobalSettings(d);
                LoadGenericTasks(d);

            }
            finally
            {
                _inLoading = false;
            }
        }

        private void LoadGenericTasks(XDocument d)
        {
            foreach (var taskNode in d.Descendants("Task").Where(t => t.Attribute("Name") != null))
            {
                try
                {
                    //TODO: Dependency Injection
                    _taskConfigurations.Add(XmlTaskConfig.FromXmlElement(taskNode));
                }
                catch (Exception ex)
                {
                    _logger.Dump(ex);
                    throw;
                }
            }
        }

        private void LoadGlobalSettings(XDocument d)
        {
            XElement globalConfig = d.Root.Element("Global");

            if (globalConfig == null)
            {
                _logger.Dump("Global Service config could not be loaded", SeverityLevel.Critical);
                throw new FormatException();
            }

            XElement logLevelElement = globalConfig.Element("LogLevel");
            if (logLevelElement != null)
                LogLevel = GetLogLevelFromString(logLevelElement.Value);

            XElement consoleLevelElement = globalConfig.Element("ConsoleLevel");
            if (consoleLevelElement != null)
                ConsoleLevel = GetLogLevelFromString(consoleLevelElement.Value);

            _logger.Dump($"Log level is set to : {LogLevel} ", SeverityLevel.Verbose);
            _logger.Dump($"Console level is set to : {ConsoleLevel} ", SeverityLevel.Verbose);

            XElement checkIntervalElement = globalConfig.Element("CheckInterval");
            if (checkIntervalElement != null)
            {
                EventLogInterval = int.Parse(checkIntervalElement.Value) * 1000;

                _logger.Dump($"Check interval is set to : {EventLogInterval / 1000} s", SeverityLevel.Verbose);
            }

            XElement banlist = globalConfig.Element("Banlist");
            if (banlist != null)
            {
                string banstring = banlist.Value;
                foreach (string ip in banstring.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    _blacklistAddresses.Add(IPAddress.Parse(ip));

                }

                _logger.Dump($"Loaded permabanlist: {banstring}", SeverityLevel.Verbose);
            }

            XElement whitelist = globalConfig.Element("WhiteList");
            if (whitelist != null)
            {
                string wstring = whitelist.Value;
                foreach (string ipPattern in wstring.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    _whiteListPatterns.Add(ipPattern);

                }
                _logger.Dump($"Loaded whitelist: {wstring}", SeverityLevel.Verbose);
            }

        }

        private SeverityLevel GetLogLevelFromString(string value)
        {
            return (SeverityLevel)Enum.Parse(typeof(SeverityLevel), value);
        }

        #endregion
    }
}
