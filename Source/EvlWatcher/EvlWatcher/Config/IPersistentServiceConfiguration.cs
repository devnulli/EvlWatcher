using EvlWatcher.Logging;
using System.Linq;
using System.Net;

namespace EvlWatcher.Config
{
    public interface IPersistentServiceConfiguration
    {
        /// <summary>
        /// Messages at or above this level will we written to the Log.
        /// </summary>
        SeverityLevel LogLevel { get; set; }

        /// <summary>
        /// Messages at or above this level will be put to the Console (if present -> i.e when you are debugging the service )
        /// </summary>
        SeverityLevel ConsoleLevel { get; set; }
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

        IQueryable<IPersistentTaskConfiguration> TaskConfigurations { get; }

        bool AddWhiteListPattern(string pattern);
        bool RemoveWhiteListPattern(string pattern);
        bool AddBlackListAddress(IPAddress address);
        bool RemoveBlackListAddress(IPAddress address);
    }
}
