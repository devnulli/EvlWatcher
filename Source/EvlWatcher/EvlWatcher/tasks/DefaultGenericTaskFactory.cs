using EvlWatcher.Config;
using EvlWatcher.Logging;
using System;
using System.Net;

namespace EvlWatcher.Tasks
{
    class DefaultGenericTaskFactory : IGenericTaskFactory
    {
        private readonly ILogger _logger;
        private readonly Func<IPAddress, bool> _isWhiteListed;

        public DefaultGenericTaskFactory(ILogger logger, Func<IPAddress, bool> isWhiteListed)
        {
            _logger = logger;
            _isWhiteListed = isWhiteListed;
        }

        public IPBlockingLogTask CreateFromConfiguration(IPersistentTaskConfiguration config)
        {
            return GenericIPBlockingTask.FromConfiguration(config, _logger, _isWhiteListed);
        }
    }
}
