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

        /// <summary>
        /// all configured generic Tasks
        /// </summary>
        IQueryable<IPersistentTaskConfiguration> TaskConfigurations { get; }

        /// <summary>
        /// adds a pattern to the white list
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns>true when a change to the whitelist was made</returns>
        bool AddWhiteListPattern(string pattern);

        /// <summary>
        /// removes a pattern from the white list
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns>true when a change to the whitelist was made</returns>
        bool RemoveWhiteListPattern(string pattern);

        /// <summary>
        /// adds an ip to the blacklist
        /// </summary>
        /// <param name="address"></param>
        /// <returns>true when a change to the blacklist was made</returns>
        bool AddBlackListAddress(IPAddress address);

        /// <summary>
        /// removes an ip from the black list
        /// </summary>
        /// <param name="address"></param>
        /// <returns>true when a change to the blacklist was made</returns>
        bool RemoveBlackListAddress(IPAddress address);

        /// <summary>
        /// forced the configuration to be reloaded, in case of external changes.
        /// </summary>
        void Load();

        /// <summary>
        /// how many lines of console output will be provided by the wcf service (max)
        /// </summary>
        int ConsoleBackLog
        {
            get;set;
        }
    }
}
