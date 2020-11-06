﻿using System.Collections.Generic;
using System.Net;

namespace EvlWatcher.Comparer
{
    public class IPAddressComparer : IComparer<IPAddress>
    {
        #region public operations

        public int Compare(IPAddress x, IPAddress y)
        {
            return x.ToString().CompareTo(y.ToString());
        }

        #endregion
    }
}
