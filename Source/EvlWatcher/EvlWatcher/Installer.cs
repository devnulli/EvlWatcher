using System.ServiceProcess;
using System.Configuration.Install;

namespace EvlWatcher
{
    [System.ComponentModel.RunInstaller(true)]
    public class LRServiceInstaller : Installer
    {
        public LRServiceInstaller()
        {
            ServiceProcessInstaller spi = new ServiceProcessInstaller();
            ServiceInstaller si = new ServiceInstaller();

            spi.Account = ServiceAccount.LocalSystem;
            spi.Username = null;
            spi.Password = null;

            si.DisplayName = "EvlWatcher";
            si.ServiceName = "EvlWatcher";
            si.Description = "Automatically monitors the event log for anomalies and acts accordingly";
            si.StartType = ServiceStartMode.Automatic;

            this.Installers.Add(spi);
            this.Installers.Add(si);
        }
    }
}
