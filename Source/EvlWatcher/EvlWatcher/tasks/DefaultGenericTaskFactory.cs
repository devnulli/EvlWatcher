using EvlWatcher.Config;
using EvlWatcher.Logging;

namespace EvlWatcher.Tasks
{
    class DefaultGenericTaskFactory : IGenericTaskFactory
    {
        private readonly ILogger _logger;

        public DefaultGenericTaskFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IPBlockingLogTask CreateFromConfiguration(IPersistentTaskConfiguration config)
        {
            return GenericIPBlockingTask.FromConfiguration(config, _logger);
        }
    }
}
