using System.Linq;

namespace EvlWatcher.Config
{
    public interface IPersistentTaskConfiguration
    {
        /// <summary>
        /// indicates whether the task is active
        /// </summary>
        bool Active { get; set; }

        /// <summary>
        /// the name of the tasks
        /// </summary>
        string TaskName { get; }

        /// <summary>
        /// the description of the tasks
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// the time to lock out the scroundlel
        /// </summary>
        int LockTime { get; set; }

        /// <summary>
        /// a flag to tell evlwatcher to not resupply already processed events
        /// </summary>
        bool OnlyNewEvents { get; set; }

        /// <summary>
        /// the time frame for the logs to be inspected
        /// </summary>
        int EventAge { get; set; }

        /// <summary>
        /// how many violations trigger a temporary ban
        /// </summary>
        int TriggerCount { get; set; }

        /// <summary>
        /// how many violations trigger a permanent ban
        /// </summary>
        int PermaBanCount { get; set; }

        /// <summary>
        /// the eventlogs to be scanned, separated by comma
        /// </summary>
        string EventPath { get; set; }

        /// <summary>
        /// regex boosters to be applied before the actual regex (for performance)
        /// </summary>
        IQueryable<string> RegexBoosters { get; }

        /// <summary>
        /// add a regex booster string
        /// </summary>
        /// <param name="regexBooster"></param>
        /// <returns></returns>
        bool AddRegexBooster(string regexBooster);

        /// <summary>
        /// remove a regex booster string
        /// </summary>
        /// <param name="regexBooster"></param>
        /// <returns></returns>
        bool RemoveRegexBooster(string regexBooster);

        /// <summary>
        /// the regex used to extract an attackers IP address.
        /// </summary>
        string Regex { get; set; }
    }
}
