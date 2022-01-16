using EvlWatcher.SystemAPI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EvlWatcher.Tests.Integration
{
    [TestClass]
    public class FirewallApiTests
    {
        [TestMethod]
        public void GetBannedIPs()
        {
            //given we have a firewall API
            var api = new FirewallAPI();

            //when we get the bannes IPs, we want a result
            var ips = api.GetBannedIPs();

            Assert.IsNotNull(ips);
        }

        [TestMethod]
        public void AddV4IP()
        {
            //given we have a firewall API, and an empty rule
            var api = new FirewallAPI();
            var list = new System.Collections.Generic.List<System.Net.IPAddress>();
            api.AdjustIPBanList(list);
            var ips = api.GetBannedIPs();
            Assert.IsNotNull(ips);
            Assert.IsTrue(ips.Count == 0);

            //when we add a some ipv4 addresses
            list.Add(System.Net.IPAddress.Parse("192.192.182.15"));
            list.Add(System.Net.IPAddress.Parse("192.192.182.16"));
            list.Add(System.Net.IPAddress.Parse("192.192.182.21"));
            api.AdjustIPBanList(list);

            //then we want that address to be in there
            ips = api.GetBannedIPs();
            Assert.IsTrue(ips.Contains("192.192.182.15/255.255.255.255"));
            Assert.IsTrue(ips.Contains("192.192.182.16/255.255.255.255"));
            Assert.IsTrue(ips.Contains("192.192.182.21/255.255.255.255"));


        }
    }
}