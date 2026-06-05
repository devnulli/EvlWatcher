using EvlWatcher.Config;

namespace EvlWatcher.Tasks
{
    public interface IGenericTaskFactory
    {
        IPBlockingLogTask CreateFromConfiguration(IPersistentTaskConfiguration config);
    }
}
