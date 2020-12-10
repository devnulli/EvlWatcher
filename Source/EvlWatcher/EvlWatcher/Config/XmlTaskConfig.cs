using EvlWatcher.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace EvlWatcher.Config
{
    internal class XmlTaskConfig : IPersistentTaskConfiguration
    {
        #region static

        internal static XmlTaskConfig FromXmlElement(XElement taskConfig, object syncObject, ILogger logger)
        {
            try
            {
                _isLoading = true;
                var newTask = new XmlTaskConfig(taskConfig.Attribute("Name").Value.Trim(), syncObject, logger)
                {
                    Active = bool.Parse(taskConfig.Attribute("Active").Value),
                    Description = taskConfig.Element("Description").Value.Trim() ?? "<indescript Task>",
                    LockTime = int.Parse(taskConfig.Element("LockTime").Value.Trim()),
                    OnlyNewEvents = bool.Parse(taskConfig.Element("OnlyNew").Value.Trim()),
                    EventAge = int.Parse(taskConfig.Element("EventAge").Value.Trim()),
                    TriggerCount = int.Parse(taskConfig.Element("TriggerCount").Value.Trim()),
                    PermaBanCount = int.Parse(taskConfig.Element("PermaBanCount").Value.Trim()),
                    EventPath = taskConfig.Element("EventPath").Value.Trim(),
                    Regex = taskConfig.Element("Regex").Value.Trim()
                };

                foreach (var regexBoosterNode in taskConfig.Element("RegexBoosters").Descendants("Booster"))
                    newTask.AddRegexBooster(regexBoosterNode.Value);


                return newTask;
            }
            finally
            {
                _isLoading = false;
            }
        }

        #endregion

        #region private members

        private readonly List<string> _regexBoosters = new List<string>();
        private static bool _isLoading = false;
        private readonly object _syncObject;
        private ILogger _logger;

        private string _description;
        private int _lockTime;
        private bool _onlyNewEvents;
        private int _eventAge;
        private int _triggerCount;
        private int _permaBanCount;
        private string _eventPath;
        private string _regex;
        private bool _active;

        #endregion

        #region private .ctor

        private XmlTaskConfig(string taskName, object syncObject, ILogger logger)
        {
            TaskName = taskName ?? throw new ArgumentNullException();
            _syncObject = syncObject;
            _logger = logger;
        }

        #endregion

        #region private operations

        private void WriteTaskConfig(string property, string value)
        {
            lock (_syncObject)
            {
                _logger.Dump($"Writing task config for task {TaskName}: {property} = {value}", SeverityLevel.Verbose);

                XDocument d = XDocument.Load(Assembly.GetExecutingAssembly().Location.Replace("EvlWatcher.exe", "config.xml"));
                XElement taskEl = d.Root.Descendants("Task").Where(t => t.Attribute("Name")!= null && t.Attribute("Name").Value == TaskName).FirstOrDefault();
                if (taskEl == null)
                {
                    throw new FormatException($"No configuration node for task {TaskName}");
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
        }

        #endregion

        #region public properties

        public string Description {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("Description", value);
                    }
                }
            }
        }
        public int LockTime
        {
            get => _lockTime;
            set
            {
                if (_lockTime != value)
                {
                    _lockTime = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("LockTime", value.ToString());
                    }
                }
            }
        }
        public bool OnlyNewEvents
        {
            get => _onlyNewEvents;
            set
            {
                if (_onlyNewEvents != value)
                {
                    _onlyNewEvents = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("OnlyNew", value.ToString());
                    }
                }
            }
        }

        public int EventAge
        {
            get => _eventAge;
            set
            {
                if (_eventAge != value)
                {
                    _eventAge = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("EventAge", value.ToString());
                    }
                }
            }
        } 
        public int TriggerCount
        {
            get => _triggerCount;
            set
            {
                if (_triggerCount != value)
                {
                    _triggerCount = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("TriggerCount", value.ToString());
                    }
                }
            }
        }

        public int PermaBanCount
        {
            get => _permaBanCount;
            set
            { 
                if (_permaBanCount != value)
                {
                    _permaBanCount = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("PermaBanCount", value.ToString());
                    }
                } 
            }
        }

        public string EventPath
        {
            get => _eventPath;
            set
            {
                if (_eventPath != value)
                {
                    _eventPath = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("EventPath", value);
                    }
                }
            }
        }

        public IQueryable<string> RegexBoosters => _regexBoosters.AsQueryable();

        public string Regex
        {
            get => _regex;
            set
            { if (_regex != value)
                {
                    _regex = value;
                    if (!_isLoading)
                    {
                        WriteTaskConfig("Regex", value.ToString());
                    }
                } }
        }

        public string TaskName { get; private set; }

        public bool Active
        {
            get => _active;
            private set
            {
                _active = value;
            }
        }

        #endregion

        #region public operations

        public bool AddRegexBooster(string regexBooster)
        {
            if (!_regexBoosters.Contains(regexBooster))
            {
                _regexBoosters.Add(regexBooster);
                return true;
            }

            return false;
        }

        public bool RemoveRegexBooster(string regexBooster)
        {
            if (_regexBoosters.Contains(regexBooster))
            {
                _regexBoosters.Remove(regexBooster);
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return TaskName;
        }

        #endregion
    }
}
