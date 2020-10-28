using System.Linq;
using System.Net;

namespace EvlWatcher.Config
{
    public interface IPersistentServiceConfiguration
    {
        /// <summary>
        /// this is the interval the log files should be checked, in seconds
        /// </summary>
        int EventLogInterval { get; set; }
        /// <summary>
        /// this is the list of adresses which never get banned
        /// </summary>
        IQueryable<string> WhitelistPatterns { get; }
        /// <summary>
        /// this is the list of adresses which should always bebanned
        /// </summary>
        IQueryable<IPAddress> BlacklistAddresses { get; }

        bool AddWhiteListPattern(string pattern);
        bool RemoveWhiteListPattern(string pattern);
        bool AddBlackListAddress(IPAddress address);
        bool RemoveBlackListAddress(IPAddress address);
    }
}
