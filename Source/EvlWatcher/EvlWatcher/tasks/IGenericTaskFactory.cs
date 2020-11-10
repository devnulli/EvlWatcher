using EvlWatcher.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvlWatcher.Tasks
{
    public interface IGenericTaskFactory
    {
        IPBlockingLogTask CreateFromConfiguration(IPersistentTaskConfiguration config);
    }
}
