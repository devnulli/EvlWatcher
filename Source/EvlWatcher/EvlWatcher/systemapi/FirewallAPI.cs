using System;
using System.Collections.Generic;
using NetFwTypeLib;
using System.Net;
using EvlWatcher.Comparer;
using System.Runtime.InteropServices;

namespace EvlWatcher.SystemAPI
{
    /// <summary>
    /// this class wraps required parts of the Microsoft Firewall with Enhanced Security COM API
    /// </summary>
    public class FirewallAPI : IDisposable
    {
        private const string CLSID_FWPOLICY2 = "{E2B3C97F-6AE1-41AC-817A-F6F92166D7DD}";
        private const string CLSID_FWRULE = "{2C5BC43E-3369-4C33-AB0C-BE9469677AF4}";
        private bool _disposed;

        private INetFwPolicy2 _fwPolicy2 = null;
        private INetFwRule _fwRule = null;

        private INetFwPolicy2 GetPolicy2()
        {
            if (_fwPolicy2 == null)
            {
                Type objectType = Type.GetTypeFromCLSID(
                    new Guid(CLSID_FWPOLICY2));
                _fwPolicy2 = Activator.CreateInstance(objectType)
                      as INetFwPolicy2;
            }

            return _fwPolicy2;
        }

        private INetFwRule GetFwRule()
        {
            if (_fwRule == null)
            {
                Type objectType = Type.GetTypeFromCLSID(new Guid(CLSID_FWRULE));
                _fwRule = Activator.CreateInstance(objectType)
                      as INetFwRule;
            }

            return _fwRule;
        }

        public void ClearIPBanList()
        {
            INetFwRule rule = GetOrCreateEvlWatcherRule(false);
            if (rule != null)
                GetPolicy2().Rules.Remove(rule.Name);
        }

        public bool AdjustIPBanList(List<IPAddress> ips)
        {
            ips.Sort(new IPAddressComparer());

            INetFwRule rule = GetOrCreateEvlWatcherRule();

            bool changed = false;

            if (ips.Count == 0)
            {
                if (rule.Enabled)
                {
                    rule.Enabled = false;
                    changed = true;
                }
            }
            else
            {
                string remoteAdresses = "";
                bool first = true;
                foreach (IPAddress s in ips)
                {
                    if (!first)
                        remoteAdresses += ",";
                    else
                        first = false;

                    if (s.ToString().Contains("."))
                        remoteAdresses += s + "/255.255.255.255";
                }

                if (rule.RemoteAddresses != remoteAdresses)
                {
                    rule.RemoteAddresses = remoteAdresses;
                    changed = true;
                }

                if (!rule.Enabled)
                {
                    rule.Enabled = true;
                    changed = true;
                }
            }

            return changed;
            
        }

        private INetFwRule GetOrCreateEvlWatcherRule()
        {
            return GetOrCreateEvlWatcherRule(true);
        }

        private INetFwRule GetOrCreateEvlWatcherRule(bool create)
        {
            INetFwPolicy2 policies = GetPolicy2();
            INetFwRule rule = null;

            bool found = false;
            foreach (INetFwRule r in policies.Rules)
            {
                if (r.Name == "EvlWatcher")
                {
                    found = true;
                    rule = r;
                    break;
                }
            }

            if (!found && create)
            {
                rule = GetFwRule();
                rule.Enabled = false;
                rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                rule.Description = "This is the rule EvlWatcher uses for temporarily banning IPs. It will enable/disable automatically when IPs need to be banned. You don't have to manually enable it.";
                rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                rule.EdgeTraversal = false;
                rule.LocalAddresses = "*";
                rule.Name = "EvlWatcher";
                rule.Profiles = 2147483647; // = means all Profiles
                rule.Protocol = 256;
                policies.Rules.Add(rule);
            }

            return rule;
        }

        public List<string> GetBannedIPs()
        {
            List<string> currentlyBannedIPs = new List<string>();

            INetFwRule fwRule = GetFwRule();

            if (fwRule.Enabled)
            {
                string remoteAddresses = fwRule.RemoteAddresses;
                if (remoteAddresses != null)
                {
                    foreach (string s in remoteAddresses.Split(','))
                        currentlyBannedIPs.Add(s);
                }
            }

            return currentlyBannedIPs;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if(_fwRule != null)
                {
                    Marshal.ReleaseComObject(_fwRule);
                    _fwRule = null;
                }

                if(_fwPolicy2 == null)
                {
                    Marshal.ReleaseComObject(_fwPolicy2);
                    _fwPolicy2 = null;
                }
                _disposed = true;
            }
        }

        
         ~FirewallAPI()
         {     
             Dispose(disposing: false);
         }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
