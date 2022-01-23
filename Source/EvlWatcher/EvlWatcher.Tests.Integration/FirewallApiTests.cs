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

        [TestMethod]
        public void AddV6IP()
        {
            //given we have a firewall API, and an empty rule
            var api = new FirewallAPI();
            var list = new System.Collections.Generic.List<System.Net.IPAddress>();
            api.AdjustIPBanList(list);
            var ips = api.GetBannedIPs();
            Assert.IsNotNull(ips);
            Assert.IsTrue(ips.Count == 0);

            //when we add a some ipv6 addresses
            list.Add(System.Net.IPAddress.Parse("48a2:ca86:e35:977c:d2dc:1276:1754:f5e6"));
            list.Add(System.Net.IPAddress.Parse("2b9c:5213:2df6:9866:5073:45c4:291:d82f"));
            list.Add(System.Net.IPAddress.Parse("692d:22df:cd31:d65b:ba37:ba83:fc5b:3d40"));
            api.AdjustIPBanList(list);

            //then we want that address to be in there
            ips = api.GetBannedIPs();
            Assert.IsTrue(ips.Contains("48a2:ca86:e35:977c:d2dc:1276:1754:f5e6"));
            Assert.IsTrue(ips.Contains("2b9c:5213:2df6:9866:5073:45c4:291:d82f"));
            Assert.IsTrue(ips.Contains("692d:22df:cd31:d65b:ba37:ba83:fc5b:3d40"));


        }

        [TestMethod]
        public void AddCombinedV4V6()
        {
            //given we have a firewall API, and an empty rule
            var api = new FirewallAPI();
            var list = new System.Collections.Generic.List<System.Net.IPAddress>();
            api.AdjustIPBanList(list);
            var ips = api.GetBannedIPs();
            Assert.IsNotNull(ips);
            Assert.IsTrue(ips.Count == 0);

            //when we add a some ipv6 addresses
            list.Add(System.Net.IPAddress.Parse("48a2:ca86:e35:977c:d2dc:1276:1754:f5e6"));
            list.Add(System.Net.IPAddress.Parse("192.192.182.15"));
            list.Add(System.Net.IPAddress.Parse("192.192.182.16"));
            list.Add(System.Net.IPAddress.Parse("2b9c:5213:2df6:9866:5073:45c4:291:d82f"));
            list.Add(System.Net.IPAddress.Parse("192.192.182.21"));
            list.Add(System.Net.IPAddress.Parse("692d:22df:cd31:d65b:ba37:ba83:fc5b:3d40"));

            api.AdjustIPBanList(list);

            //then we want that address to be in there
            ips = api.GetBannedIPs();
            Assert.IsTrue(ips.Contains("48a2:ca86:e35:977c:d2dc:1276:1754:f5e6"));
            Assert.IsTrue(ips.Contains("2b9c:5213:2df6:9866:5073:45c4:291:d82f"));
            Assert.IsTrue(ips.Contains("692d:22df:cd31:d65b:ba37:ba83:fc5b:3d40"));
            Assert.IsTrue(ips.Contains("192.192.182.15/255.255.255.255"));
            Assert.IsTrue(ips.Contains("192.192.182.16/255.255.255.255"));
            Assert.IsTrue(ips.Contains("192.192.182.21/255.255.255.255"));


        }

    }
}