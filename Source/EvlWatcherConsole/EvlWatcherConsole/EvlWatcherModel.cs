using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.ServiceModel;
using System.Net;
using System.Windows.Input;
using EvlWatcherConsole.MVVMBase;
using EvlWatcher.WCF;
using System.Xml.Schema;
using EvlWatcher.Logging;

namespace EvlWatcherConsole
{
    public class EvlWatcherModel : GuiObject
    {
        #region private members

        private Thread _updater = null;
        private static volatile bool _run = true;
        private object syncObject = new object();

        private int _lockTime = -1;
        private int _timeFrame = -1;
        private int _triggerCount = -1;
        private int _permaBanTrigger = -1;

        private bool _isRunning = false;
        private string _permaBanIPString = "";
        private string _whiteListFilter = "";
        private string _consoleText;
        private SeverityLevel _selectedConsoleLevel = SeverityLevel.Verbose;

        #endregion

        #region constructor / destructor

        public EvlWatcherModel()
        {
            StartUpdating();
        }

        ~EvlWatcherModel()
        {
            StopUpdating();
        }

        #endregion

        #region private operations

        private void AddWhiteListEntry(string s)
        {
            lock (syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.AddWhiteListEntry(s);
            }
        }

        public  IList<SeverityLevel> ConsoleLevels
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

        private void RemoveWhiteListEntry(string s)
        {
            lock (syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.RemoveWhiteListEntry(s);
            }
        }

        private void AddPermanentIPBan(IPAddress a)
        {
            lock (syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.SetPermanentBan(a);
            }
        }

        private void RemovePermanentIPBan(IPAddress a)
        {
            lock (syncObject)
            {
                ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                IEvlWatcherService service = f.CreateChannel();
                service.ClearPermanentBan(a);
            }
        }

        private void StartUpdating()
        {
            _updater = new Thread(new ThreadStart(this.Run))
            {
                IsBackground = true
            };
            _updater.Start();
            _run = true;
        }

        private void StopUpdating()
        {
            try
            {
                _run = false;
                _updater.Interrupt();
                DateTime waiting = DateTime.Now;
                while (_updater.IsAlive && DateTime.Now.Subtract(waiting).TotalSeconds < 3)
                {
                    Thread.Sleep(100);
                }
            }
            catch
            { }
        }

        private void Run()
        {
            while (_run)
            {
                lock (syncObject)
                {
                    //do not update in design mode
                    
                    bool running = false;

                    try
                    {
                        var binding = new NetNamedPipeBinding()
                        {
                            MaxReceivedMessageSize = Int32.MaxValue //Setting to receive big console logs
                        };

                        ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(binding, new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                        IEvlWatcherService service = f.CreateChannel();

                        running = service.GetIsRunning();

                        if (!running)
                            continue;

                        if (IsIPTabSelected)
                        {
                            UpdateIPLists(service);
                            UpdateWhileListPattern(service);
                        }
                        if (IsConsoleTabSelected)
                        {
                            UpdateConsole(service);
                        }

                        f.Close();
                    }
                    catch (FaultException<ExceptionFaultContract> ex)
                    {
                        MessageBox.Show(ex.Detail.Message, $"Error Code: {ex.Detail.Code}", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (ex.Detail.CanTerminate)
                            Environment.Exit(0);
                    }
                    catch (EndpointNotFoundException)
                    {
                        //service seems not to be running
                    }
                    catch (TimeoutException)
                    {
                        // same here.. well, would be nice if exception filters would have been invented by now...
                    }
                    finally
                    {
                        IsRunning = running;
                    }
                }
                try
                {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }   
            }
        }

        private void UpdateConsole(IEvlWatcherService service)
        {
            if (_selectedConsoleLevel == SeverityLevel.Off)
                return;

            var data = service.GetConsoleHistory();
            var sb = new StringBuilder();

            foreach (var log in data.Where(l => l.Severity >= _selectedConsoleLevel))
                sb.AppendLine($"{log.Date} - [{log.Severity}]: {log.Message}");

            ConsoleText = sb.ToString();
        }

        private void UpdateWhileListPattern(IEvlWatcherService service)
        {
            List<string> entries = new List<string>(service.GetWhiteListEntries());
            List<string> toAdd = new List<string>();
            List<string> toRemove = new List<string>();

            foreach (string s in entries)
            {
                if (!WhiteListedIPs.Contains(s))
                    toAdd.Add(s);
            }
            foreach (string s in WhiteListedIPs)
            {
                if (!entries.Contains(s))
                    toRemove.Add(s);
            }
            foreach (string s in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => WhiteListedIPs.Add(s)));

            foreach (string s in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => WhiteListedIPs.Remove(s)));
        }

        private void UpdateIPLists(IEvlWatcherService service)
        {
            List<IPAddress> ips = new List<IPAddress>(service.GetTemporarilyBannedIPs());
            List<IPAddress> toAdd = new List<IPAddress>();
            List<IPAddress> toRemove = new List<IPAddress>();

            foreach (IPAddress i in ips)
            {
                if (!TemporarilyBannedIPs.Contains(i))
                    toAdd.Add(i);
            }
            foreach (IPAddress i in TemporarilyBannedIPs)
            {
                if (!ips.Contains(i))
                    toRemove.Add(i);
            }
            foreach (IPAddress i in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => TemporarilyBannedIPs.Add(i)));

            foreach (IPAddress i in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => TemporarilyBannedIPs.Remove(i)));

            ips = new List<IPAddress>(service.GetPermanentlyBannedIPs());
            toAdd = new List<IPAddress>();
            toRemove = new List<IPAddress>();
            foreach (IPAddress i in ips)
            {
                if (!PermanentlyBannedIPs.Contains(i))
                    toAdd.Add(i);
            }
            foreach (IPAddress i in PermanentlyBannedIPs)
            {
                if (!ips.Contains(i))
                    toRemove.Add(i);
            }
            foreach (IPAddress i in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => PermanentlyBannedIPs.Add(i)));

            foreach (IPAddress i in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => PermanentlyBannedIPs.Remove(i)));
        }

        #endregion

        #region public properties

        public ICommand MoveTemporaryToPermaCommand
        {
            get
            {
                return new RelayCommand(p => { AddPermanentIPBan(SelectedTemporaryIP); }, p => { return SelectedTemporaryIP != null; });
            }
        }

        public bool IsIPTabSelected
        {
            get; set;
        }

        public bool IsConsoleTabSelected
        {
            get;set;
        }

        public bool IsRuleEditorTabSelected
        {
            get; set;
        }

        public ICommand MoveTemporaryToWhiteListCommand
        {
            get
            {
             return new RelayCommand(p => { AddWhiteListEntry(SelectedTemporaryIP.ToString()); }, p => { return SelectedTemporaryIP != null; });
            }
        }

        public IPAddress SelectedTemporaryIP
        {
            get;
            set;
        }

        public SeverityLevel SelectedConsoleLevel
        {
            get
            {
                return _selectedConsoleLevel;
            }
            set
            {
                _selectedConsoleLevel = value;
                Notify(nameof(SelectedConsoleLevel));
            }
        }

        public IPAddress SelectedPermanentIP
        {
            get;
            set;
        }

        public string SelectedWhiteListPattern
        {
            get;
            set;
        }

        public ICommand AddPermaBanCommand
        {
            get
            {
                return new RelayCommand( p =>
                    { AddPermanentIPBan(IPAddress.Parse(PermaBanIPString)); PermaBanIPString = ""; }, p => { IPAddress dummy; return IPAddress.TryParse(PermaBanIPString, out dummy); });
            }
        }

        public ICommand RemovePermaBanCommand
        {
            get
            {
                return new RelayCommand(p => RemovePermanentIPBan(SelectedPermanentIP), p => { return SelectedPermanentIP != null; });
            }
        }

        public string WhiteListFilter
        {
            get
            {
                return _whiteListFilter;
            }

            set
            {
                _whiteListFilter = value;
                Notify(nameof(WhiteListFilter));
            }
        }

        public ICommand AddWhiteListFilterCommand
        {
            get
            {
                //TODO create execute predicate
                return new RelayCommand(p => { AddWhiteListEntry(WhiteListFilter); WhiteListFilter = ""; }, p => { return WhiteListFilter.Length > 0; });
            }
        }

        public ICommand RemoveWhiteListFilterCommand
        {
            get
            {
                return new RelayCommand(p => { RemoveWhiteListEntry(SelectedWhiteListPattern); }, p => { return SelectedWhiteListPattern != null; });
            }
        }

        public string PermaBanIPString
        {
            get
            {
                return _permaBanIPString;
            }

            set
            {
                _permaBanIPString = value;
                Notify(nameof(PermaBanIPString));
            }
        }

        public int PermaBanCount
        {
            get
            {
                return _permaBanTrigger;
            }
            set
            {
                lock (syncObject)
                {
                    ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                    IEvlWatcherService service = f.CreateChannel();
                    _permaBanTrigger = value;
                    Notify(nameof(PermaBanCount));
                }
            }
        }

        public int TriggerCount
        {
            get
            {
                return _triggerCount;
            }
            set
            {
                lock (syncObject)
                {
                    ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                    IEvlWatcherService service = f.CreateChannel();
                    _triggerCount = value;
                    Notify(nameof(TriggerCount));
                }
            }
        }

        public int TimeFrame
        {
            get
            {
                return _timeFrame;
            }
            set
            {
                lock (syncObject)
                {
                    ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                    IEvlWatcherService service = f.CreateChannel();
                    _timeFrame = value;
                    Notify(nameof(TimeFrame));
                }
            }
        }

        public int LockTime
        {
            get
            {
                return _lockTime;
            }
            set
            {
                lock (syncObject)
                {
                    ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
                    IEvlWatcherService service = f.CreateChannel();
                    _lockTime = value;
                    Notify(nameof(LockTime));
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }

            private set
            {
                _isRunning = value;
                Notify(nameof(IsRunning));
            }
        }

        public ObservableCollection<IPAddress> TemporarilyBannedIPs { get; } = new ObservableCollection<IPAddress>();
        public ObservableCollection<IPAddress> PermanentlyBannedIPs { get; } = new ObservableCollection<IPAddress>();
        public ObservableCollection<string> WhiteListedIPs { get; } = new ObservableCollection<string>();

        public string ConsoleText
        {
            get
            {
                return _consoleText;
            }
            set
            {
                _consoleText = value;
                Notify(nameof(ConsoleText));
            }
        }

        public ObservableCollection<LogEntry> ConsoleHistory { get; } = new ObservableCollection<LogEntry>();

        #endregion
    }
}
