using EvlWatcher.Logging;
using EvlWatcher.WCF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EvlWatcherConsole.Model
{
    public class EvlWatcherModel
    {
        #region private members

        private readonly object _syncObject = new object();

        #endregion

        #region public properties

        public IList<SeverityLevel> ConsoleLevels
        {
            get
            {
                return new List<SeverityLevel>()
                    { SeverityLevel.Off,
                    SeverityLevel.Debug,
                    SeverityLevel.Verbose,
                    SeverityLevel.Info,
                    SeverityLevel.Warning,
                    SeverityLevel.Error,
                    SeverityLevel.Critical };
            }
        }

        public bool IsServiceResponding
        {
            get
            {
                return Service.GetIsRunning();
            }
        }
        #endregion

        #region public operations

        public IQueryable<IPAddress> GetTemporarilyBannedIPs()
        {
            return Service.GetTemporarilyBannedIPs().AsQueryable();
        }

        public IQueryable<IPAddress> GetPermanentlyBannedIPs()
        {
            return Service.GetPermanentlyBannedIPs().AsQueryable();
        }

        public IQueryable<string> GetWhiteListPatterns()
        {
            return Service.GetWhiteListEntries().AsQueryable();
        }

        public IQueryable<LogEntry> GetConsoleHistory(SeverityLevel severityLevel)
        {
            if (severityLevel == SeverityLevel.Off)
            {
                return new List<LogEntry>().AsQueryable();
            }
            else
            {
                return Service.GetConsoleHistory().Where(entry => entry.Severity >= severityLevel).AsQueryable();
            }
        }

        public void AddWhiteListEntry(string s)
        {
            lock (_syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.AddWhiteListEntry(s);
            }
        }

        public void RemoveWhiteListEntry(string s)
        {
            lock (_syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.RemoveWhiteListEntry(s);
            }
        }

        public void AddPermanentIPBan(IPAddress a)
        {
            lock (_syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.SetPermanentBan(a);
            }
        }

        public void RemovePermanentIPBan(IPAddress a)
        {
            lock (_syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.ClearPermanentBan(a);
            }
        }
        #endregion

        #region private operations

        private IEvlWatcherService Service
        {
            get
            {
                var binding = new NetNamedPipeBinding()
                {
                    MaxReceivedMessageSize = Int32.MaxValue //Setting to receive big console logs
                };

                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(binding, new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                return f.CreateChannel();
            }
        }

        #endregion
    }
}
