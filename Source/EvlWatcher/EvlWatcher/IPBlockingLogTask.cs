using System.Collections.Generic;
using System.Net;

namespace EvlWatcher
{
    /// <summary>
    /// this it the base class that should be used by log tasks that are able to block IPs
    /// </summary>
    public abstract class IPBlockingLogTask : LogTask
    {
        public abstract List<IPAddress> GetTempBanVictims();
        public abstract List<IPAddress> GetPermaBanVictims();
    }
}
