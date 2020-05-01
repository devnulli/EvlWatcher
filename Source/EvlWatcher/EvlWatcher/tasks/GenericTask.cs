using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EvlWatcher.Tasks
{
    internal class GenericTask : IPBlockingLogTask
    {
        #region static
        internal static GenericTask FromXML(XElement element)
        {
            GenericTask t =
            new GenericTask()
            {
                Name = element.Name.LocalName
            };

            t.Description = element.Element("Description").Value.Trim();
            t._lockTime = int.Parse(element.Element("LockTime").Value.Trim());
            t.OnlyNew = bool.Parse(element.Element("OnlyNew").Value.Trim());
            t.EventAge = int.Parse(element.Element("EventAge").Value.Trim());
            t._triggerCount = int.Parse(element.Element("TriggerCount").Value.Trim());
            t._permaBanTrigger = int.Parse(element.Element("PermaBanCount").Value.Trim());
            t.EventPath= element.Element("EventPath").Value.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (XElement e in element.Element("RegexBoosters").Elements("Booster"))
                t._boosters.Add(e.Value.Trim());
            try
            {
                t._regex = new Regex(element.Element("Regex").Value.Trim(), RegexOptions.Compiled);
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
        private List<string> _boosters = new List<string>();

        private int _lockTime = 3600; //lock time in seconds
        private int _triggerCount = 5;
        private int _permaBanTrigger = 3;

        private Regex _regex = null;

        #endregion

        #region internal .ctor

        internal GenericTask() { }

        #endregion

        #region public operations

        public override List<IPAddress> GetTempBanVictims()
        {
            List<IPAddress> ipsToRemove = new List<IPAddress>();
            List<IPAddress> ipsToBlock = new List<IPAddress>();

            //also remove IPS from ban list when they have been blocked "long enough"
            foreach (KeyValuePair<IPAddress, DateTime> kvp in _blockedIPsToDate)
            {
                if (kvp.Value.Add(new TimeSpan(0, 0, _lockTime)) < System.DateTime.Now)
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
                if (kvp.Value >= _permaBanTrigger)
                    permaList.Add(kvp.Key);
            }
            foreach (IPAddress ip in permaList)
                _bannedCount.Remove(ip);

            return permaList;
        }

        protected override void OnComputeEvents(List<EventRecord> events)
        {
            Dictionary<IPAddress, int> sourceToCount = new Dictionary<IPAddress, int>();
            foreach (EventRecord e in events)
            {
                string xml = e.ToXml();

                bool abort = false;
                foreach (string b in _boosters)
                    if (!xml.Contains(b))
                    {
                        abort = true;
                        break;
                    }
                if (abort)
                    continue;

                Match m = _regex.Match(xml);

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
                if (kvp.Value >= _triggerCount && !_blockedIPsToDate.ContainsKey(kvp.Key))
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
