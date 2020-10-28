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

                    WriteConfig("GLOBAL", "WhiteList", s);
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

                    WriteConfig("GLOBAL", "WhiteList", s);
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

        private readonly ILogger _logger;


        #endregion

        #region private operations

        private void WriteConfig(string task, string property, string value)
        {
            _logger.Dump($"Writing config for: {task}: {property} = {value}", SeverityLevel.Verbose);

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


        private void LoadConfiguration()
        {
            XDocument d = XDocument.Load(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config\\config.xml"));

            LoadGlobalSettings(d);
        }

        private void LoadGlobalSettings(XDocument d)
        {
            XElement debugModeElement = d.Root.Element("DebugMode");
            if (debugModeElement != null)
            {
                SetLogLevel(debugModeElement.Value);
            }

            XElement checkIntervalElement = d.Root.Element("CheckInterval");
            if (checkIntervalElement != null)
            {
                EventLogInterval = int.Parse(checkIntervalElement.Value) * 1000;
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
                        _blacklistAddresses.Add(IPAddress.Parse(ip));

                    }

                    _logger.Dump($"Loaded permabanlist: {banstring}", SeverityLevel.Info);
                }

                XElement whitelist = globalConfig.Element("WhiteList");
                if (whitelist != null)
                {
                    string wstring = whitelist.Value;
                    foreach (string ipPattern in wstring.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _whiteListPatterns.Add(ipPattern);

                    }
                    _logger.Dump($"Loaded whitelist: {wstring}", SeverityLevel.Info);
                }
            }
        }

        private void SetLogLevel(string value)
        {
            _logger.SetLogLevel((SeverityLevel)Enum.Parse(typeof(SeverityLevel), value));
        }

        #endregion
    }
}
