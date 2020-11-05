using EvlWatcher.DTOs;
using System;
using System.Collections.Generic;

namespace EvlWatcher.Tasks
{
    /// <summary>
    /// this is the base class for every task that EvlWatcher should perform 
    /// </summary>
    public abstract class LogTask
    {
        /// <summary>
        /// The name of the task
        /// </summary>
        public string Name;

        /// <summary>
        /// The description of the task
        /// </summary>
        public string Description;

        /// <summary>
        /// The maximum age of the events that make it into the bundle that is sent to the task, in seconds, (when OnlyNew is set to false)
        /// </summary>
        public int EventAge = 120;

        /// <summary>
        /// With this set, the task receives only new events, with this off the task receives events that are younger than the event age (means the task will receive events twice or more)
        /// </summary>
        public bool OnlyNew = false;

        public DateTime LastCalled = DateTime.MinValue;

        /// <summary>
        /// This is a list of logs the event wants to receive ("Security, Application")
        /// </summary>
        public IList<string> EventPath = new List<string>();

        public void ProvideEvents(List<ExtractedEventRecord> events)
        {
            OnComputeEvents(events);
        }

        protected abstract void OnComputeEvents(List<ExtractedEventRecord> events);
    }
}
