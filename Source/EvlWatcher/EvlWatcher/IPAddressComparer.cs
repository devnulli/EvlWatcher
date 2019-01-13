using System.Collections.Generic;
using System.Net;

namespace EvlWatcher
{
    public class IPAddressComparer : IComparer<IPAddress>
    {
        public int Compare(IPAddress x, IPAddress y)
        {
            return x.ToString().CompareTo(y.ToString());
        }
    }
}
