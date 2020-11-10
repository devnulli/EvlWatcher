using System.Collections.Generic;

namespace EvlWatcher.WCF.DTO
{
    public class GenericIPBlockingTaskDTO
    {

        /// <summary>
        /// indicates whether the task is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// the name of the tasks
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// the description of the tasks
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// the time to lock out the scroundlel
        /// </summary>
        public int LockTime { get; set; }

        /// <summary>
        /// a flag to tell evlwatcher to not resupply already processed events
        /// </summary>
        public bool OnlyNewEvents { get; set; }

        /// <summary>
        /// the time frame for the logs to be inspected
        /// </summary>
        public int EventAge { get; set; }

        /// <summary>
        /// how many violations trigger a temporary ban
        /// </summary>
        public int TriggerCount { get; set; }

        /// <summary>
        /// how many violations trigger a permanent ban
        /// </summary>
        public int PermaBanCount { get; set; }

        /// <summary>
        /// the eventlogs to be scanned, separated by comma
        /// </summary>
        public IList<string> EventPath { get; set; }

        /// <summary>
        /// regex boosters to be applied before the actual regex (for performance)
        /// </summary>
        public IList<string> RegexBoosters { get; set; }

        /// <summary>
        /// the regex used to extract an attackers IP address.
        /// </summary>
        public string Regex { get; set; }

        public override bool Equals(object obj)
        {
            return obj is GenericIPBlockingTaskDTO dTO &&
                   TaskName == dTO.TaskName;
        }

        public override string ToString()
        {
            return TaskName;
        }

        public override int GetHashCode()
        {
            return 1575259903 + EqualityComparer<string>.Default.GetHashCode(TaskName);
        }
    }
}
