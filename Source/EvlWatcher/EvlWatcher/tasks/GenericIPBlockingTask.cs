using EvlWatcher.Config;
using EvlWatcher.DTOs;
using EvlWatcher.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace EvlWatcher.Tasks
{
    internal class GenericIPBlockingTask : IPBlockingLogTask
    {
        #region static

        internal static GenericIPBlockingTask FromConfiguration(IPersistentTaskConfiguration configuration, ILogger logger)
        {
            GenericIPBlockingTask t = new GenericIPBlockingTask(logger)
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

        #endregion

        #region private members

        private readonly object _syncObject = new object();
        private readonly Dictionary<IPAddress, DateTime> _blockedIPsToDate = new Dictionary<IPAddress, DateTime>();
        private readonly Dictionary<IPAddress, DateTime> _forgetIPsToDate = new Dictionary<IPAddress, DateTime>();
        private readonly Dictionary<IPAddress, int> _bannedCount = new Dictionary<IPAddress, int>();
        private readonly ILogger _logger;

        #endregion

        #region internal .ctor

        internal GenericIPBlockingTask(ILogger logger) 
        {
            _logger = logger;
        }

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
            lock (_syncObject)
            {
                List<IPAddress> ipsToRemove = new List<IPAddress>();
                List<IPAddress> ipsToBlock = new List<IPAddress>();

                //also remove IPS from ban list when they have been blocked "long enough"
                foreach (KeyValuePair<IPAddress, DateTime> kvp in _blockedIPsToDate)
                {
                    if (kvp.Value.Add(new TimeSpan(0, 0, LockTime)) < DateTime.Now)
                    {
                        ipsToRemove.Add(kvp.Key);
                    }
                    else
                    {
                        ipsToBlock.Add(kvp.Key);
                    }
                }

                //also remove forgotten IPs when its been a while
                List<IPAddress> removeFromForgottenList = _forgetIPsToDate.Where(p => DateTime.Now.AddHours(-1) > p.Value).Select(p=>p.Key).ToList();
                foreach (var ip in removeFromForgottenList)
                    removeFromForgottenList.Remove(ip);

                foreach (IPAddress ipToRemove in ipsToRemove)
                    _blockedIPsToDate.Remove(ipToRemove);

                return ipsToBlock;
            }
        }

        public override List<IPAddress> GetPermaBanVictims()
        {
            lock (_syncObject)
            {
                List<IPAddress> permaList = new List<IPAddress>();
                foreach (KeyValuePair<IPAddress, int> kvp in _bannedCount.Where(p => p.Value >= PermaBanCount))
                {
                    permaList.Add(kvp.Key);
                    _logger.Dump($"Permanently banned {kvp.Value} (strike count was over {PermaBanCount}) ", SeverityLevel.Info);
                }
                foreach (IPAddress ip in permaList)
                    _bannedCount.Remove(ip);

                return permaList;
            }
        }

        protected override void OnComputeEvents(List<ExtractedEventRecord> events)
        {
            Dictionary<IPAddress, int> sourceToCount = new Dictionary<IPAddress, int>();
            foreach (ExtractedEventRecord e in events)
            {
                _logger.Dump($"{Name}: Processing Event with timestamp {e.TimeCreated}", SeverityLevel.Debug);

                string xml = e.Xml;

                _logger.Dump($"Checking XML {xml} against boosters..", SeverityLevel.Debug);

                bool abort = false;
                foreach (string b in Boosters)
                {
                    _logger.Dump($"Booster: {b}", SeverityLevel.Debug);
                    if (!xml.Contains(b))
                    {
                        _logger.Dump($"Booster not in XML, aborting.", SeverityLevel.Debug);
                        abort = true;
                        break;
                    }
                }
                if (abort)
                    continue;


                _logger.Dump($"Checking XML against Regex {Regex} now..", SeverityLevel.Debug);
                Match m = Regex.Match(xml);

                if(m.Success)
                {
                    if (m.Groups.Count == 2 && IPAddress.TryParse(m.Groups[1].Value, out IPAddress ipAddress))
                    {
                        if (_forgetIPsToDate.ContainsKey(ipAddress))
                        {
                            _logger.Dump($"{Name}: found {ipAddress} but ignored it (was recently removed from autoban list)", SeverityLevel.Info);
                            continue;
                        }
                        
                        if (!sourceToCount.ContainsKey(ipAddress))
                            sourceToCount.Add(ipAddress, 1);
                        else
                            sourceToCount[ipAddress]++;

                        _logger.Dump($"{Name}: found {ipAddress}, trigger count is {sourceToCount[ipAddress]}", SeverityLevel.Info);
                    }
                }
            }

            lock (_syncObject)
            {
                foreach (KeyValuePair<IPAddress, int> kvp in sourceToCount)
                {
                    if (kvp.Value >= TriggerCount && !_blockedIPsToDate.ContainsKey(kvp.Key))
                    {
                        _blockedIPsToDate.Add(kvp.Key, DateTime.Now);
                        if (!_bannedCount.ContainsKey(kvp.Key))
                            _bannedCount[kvp.Key] = 1;
                        else
                            _bannedCount[kvp.Key] += 1;

                        _logger.Dump($"Temporarily banning {kvp.Key}, this is strike {_bannedCount[kvp.Key]}", SeverityLevel.Info);
                    }
                }
            }
        }

        public override void Forget(IPAddress address)
        {
            lock (_syncObject)
            {
                _blockedIPsToDate.Remove(address);

                if (!_forgetIPsToDate.ContainsKey(address))
                    _forgetIPsToDate.Add(address, DateTime.Now);

                _bannedCount.Remove(address);
            }
        }

        #endregion
    }
}
