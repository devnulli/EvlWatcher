using EvlWatcher.Config;
using EvlWatcher.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EvlWatcher.Tasks
{
    internal class GenericIPBlockingTask : IPBlockingLogTask
    {
        #region static

        internal static GenericIPBlockingTask FromConfiguration(IPersistentTaskConfiguration configuration)
        {
            GenericIPBlockingTask t = new GenericIPBlockingTask()
            {
                Name = configuration.TaskName,
                Description = configuration.Description,
                LockTime = configuration.LockTime,
                OnlyNew = configuration.OnlyNewEvents,
                EventAge = configuration.EventAge,
                TriggerCount = configuration.TriggerCount,
                PermaBanCount = configuration.PermaBanCount,
                EventPath = configuration.EventPath.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                Boosters = configuration.RegexBoosters.ToList(),
                Regex = new Regex(configuration.Regex, RegexOptions.Compiled)
            };

            return t;
        }

        internal static GenericIPBlockingTask FromXML(XElement element)
        {
            GenericIPBlockingTask t =
            new GenericIPBlockingTask()
            {
                Name = element.Name.LocalName
            };

            t.Description = element.Element("Description").Value.Trim();
            t.LockTime = int.Parse(element.Element("LockTime").Value.Trim());
            t.OnlyNew = bool.Parse(element.Element("OnlyNew").Value.Trim());
            t.EventAge = int.Parse(element.Element("EventAge").Value.Trim());
            t.TriggerCount = int.Parse(element.Element("TriggerCount").Value.Trim());
            t.PermaBanCount = int.Parse(element.Element("PermaBanCount").Value.Trim());
            t.EventPath= element.Element("EventPath").Value.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (XElement e in element.Element("RegexBoosters").Elements("Booster"))
                t.Boosters.Add(e.Value.Trim());
            try
            {
                t.Regex = new Regex(element.Element("Regex").Value.Trim(), RegexOptions.Compiled);
            }
            catch
            {
                throw new ArgumentException($"Regex not defined or invalid for Task: {t.Name}");
            }
            return t;
        }

        #endregion

        #region private members

        private Dictionary<IPAddress, DateTime> _blockedIPsToDate = new Dictionary<IPAddress, DateTime>();
        private Dictionary<IPAddress, int> _bannedCount = new Dictionary<IPAddress, int>();

        #endregion

        #region internal .ctor

        internal GenericIPBlockingTask() { }

        #endregion

        #region public properties

        public int LockTime { get; set; } = 3600;
        public List<string> Boosters { get; set; } = new List<string>();
        public int PermaBanCount { get; set; } = 3;
        public int TriggerCount { get; set; } = 5;
        public Regex Regex { get; set; } = null;

        #endregion

        #region public operations
        public override List<IPAddress> GetTempBanVictims()
        {
            List<IPAddress> ipsToRemove = new List<IPAddress>();
            List<IPAddress> ipsToBlock = new List<IPAddress>();

            //also remove IPS from ban list when they have been blocked "long enough"
            foreach (KeyValuePair<IPAddress, DateTime> kvp in _blockedIPsToDate)
            {
                if (kvp.Value.Add(new TimeSpan(0, 0, LockTime)) < System.DateTime.Now)
                {
                    ipsToRemove.Add(kvp.Key);
                }
                else
                {
                    ipsToBlock.Add(kvp.Key);
                }
            }

            foreach (IPAddress ipToRemove in ipsToRemove)
                _blockedIPsToDate.Remove(ipToRemove);

            return ipsToBlock;
        }

        public override List<IPAddress> GetPermaBanVictims()
        {
            List<IPAddress> permaList = new List<IPAddress>();
            foreach (KeyValuePair<IPAddress, int> kvp in _bannedCount)
            {
                if (kvp.Value >= PermaBanCount)
                    permaList.Add(kvp.Key);
            }
            foreach (IPAddress ip in permaList)
                _bannedCount.Remove(ip);

            return permaList;
        }

        protected override void OnComputeEvents(List<ExtractedEventRecord> events)
        {
            Dictionary<IPAddress, int> sourceToCount = new Dictionary<IPAddress, int>();
            foreach (ExtractedEventRecord e in events)
            {
                string xml = e.Xml;

                bool abort = false;
                foreach (string b in Boosters)
                    if (!xml.Contains(b))
                    {
                        abort = true;
                        break;
                    }
                if (abort)
                    continue;

                Match m = Regex.Match(xml);

                if(m.Success)
                {
                    if (m.Groups.Count == 2 && IPAddress.TryParse(m.Groups[1].Value, out IPAddress ipAddress))
                    {
                        if (!sourceToCount.ContainsKey(ipAddress))
                            sourceToCount.Add(ipAddress, 1);
                        else
                            sourceToCount[ipAddress]++;
                    }
                }
            }

            foreach (KeyValuePair<IPAddress, int> kvp in sourceToCount)
            {
                if (kvp.Value >= TriggerCount && !_blockedIPsToDate.ContainsKey(kvp.Key))
                {
                    _blockedIPsToDate.Add(kvp.Key, System.DateTime.Now);
                    if (!_bannedCount.ContainsKey(kvp.Key))
                        _bannedCount[kvp.Key] = 1;
                    else
                        _bannedCount[kvp.Key] += 1;
                }
            }
        }

        #endregion
    }
}
