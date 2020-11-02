using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EvlWatcher.Config
{
    internal class XmlTaskConfig : IPersistentTaskConfiguration
    {
        private readonly List<string> _regexBoosters = new List<string>();

        private XmlTaskConfig(string taskName)
        {
            TaskName = taskName ?? throw new ArgumentNullException();
        }

        internal static XmlTaskConfig FromXmlElement(XElement taskConfig)
        {
            var newTask = new XmlTaskConfig(taskConfig.Attribute("Name").Value.Trim())
            {
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

        public string Description { get; set; }
        public int LockTime { get; set; }
        public bool OnlyNewEvents { get; set; }
        public int EventAge { get; set; }
        public int TriggerCount { get; set; }
        public int PermaBanCount { get; set; }
        public string EventPath { get; set; }

        public IQueryable<string> RegexBoosters => _regexBoosters.AsQueryable();

        public string Regex { get; set; }

        public string TaskName { get; private set; }

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
    }
}
