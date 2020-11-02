using EvlWatcher.Config;

namespace EvlWatcher.Tasks
{
    class DefaultGenericTaskFactory : IGenericTaskFactory
    {
        public IPBlockingLogTask CreateFromConfiguration(IPersistentTaskConfiguration config)
        {
            return GenericIPBlockingTask.FromConfiguration(config);
        }
    }
}
