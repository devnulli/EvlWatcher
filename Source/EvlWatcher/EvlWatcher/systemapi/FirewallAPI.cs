using System;
using System.Collections.Generic;
using NetFwTypeLib;
using System.Net;
using EvlWatcher.Comparer;

namespace EvlWatcher.SystemAPI
{
    /// <summary>
    /// this class wraps required parts of the Microsoft Firewall with Enhanced Security COM API
    /// </summary>
    public static class FirewallAPI
    {
        private const string CLSID_FWPOLICY2 = "{E2B3C97F-6AE1-41AC-817A-F6F92166D7DD}";
        private const string CLSID_FWRULE = "{2C5BC43E-3369-4C33-AB0C-BE9469677AF4}";

        private static INetFwPolicy2 GetPolicy2()
        {
            Type objectType = Type.GetTypeFromCLSID(
                new Guid(CLSID_FWPOLICY2));
            return Activator.CreateInstance(objectType)
                  as INetFwPolicy2;
        }

        private static INetFwRule GetFwRule()
        {
            Type objectType = Type.GetTypeFromCLSID(new Guid(CLSID_FWRULE));
            return Activator.CreateInstance(objectType)
                  as INetFwRule;
        }

        public static void ClearIPBanList()
        {
            INetFwRule rule = GetOrCreateEvlWatcherRule(false);
            if (rule != null)
                GetPolicy2().Rules.Remove(rule.Name);
        }

        public static bool AdjustIPBanList(List<IPAddress> ips)
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

        private static INetFwRule GetOrCreateEvlWatcherRule()
        {
            return GetOrCreateEvlWatcherRule(true);
        }

        private static INetFwRule GetOrCreateEvlWatcherRule(bool create)
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

        public static List<string> GetBannedIPs()
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
    }
}
