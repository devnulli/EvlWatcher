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

namespace EvlWatcherConsole
{
    public class EvlWatcherController : GuiObject
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
        private ObservableCollection<IPAddress> _temporarilyBannedIps = new ObservableCollection<IPAddress>();
        private ObservableCollection<IPAddress> _permanentlyBannedIps = new ObservableCollection<IPAddress>();
        private ObservableCollection<string> _whiteListPattern = new ObservableCollection<string>();

        private string _permaBanIPString = "";
        private string _whiteListFilter = "";
        private string _consoleText;

        #endregion

        #region constructor / destructor

        public EvlWatcherController()
        {
            StartUpdating();
        }

        ~EvlWatcherController()
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
                        ChannelFactory<IEvlWatcherService> f = new ChannelFactory<IEvlWatcherService>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/EvlWatcher"));
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
            ConsoleText += $"\n{DateTime.Now}: working on it, will be part of v2.0 release.. (github #24, #25) {new Random().Next(1000)} ";
        }

        private void UpdateWhileListPattern(IEvlWatcherService service)
        {
            List<string> entries = new List<string>(service.GetWhiteListEntries());
            List<string> toAdd = new List<string>();
            List<string> toRemove = new List<string>();

            foreach (string s in entries)
            {
                if (!_whiteListPattern.Contains(s))
                    toAdd.Add(s);
            }
            foreach (string s in _whiteListPattern)
            {
                if (!entries.Contains(s))
                    toRemove.Add(s);
            }
            foreach (string s in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => _whiteListPattern.Add(s)));

            foreach (string s in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => _whiteListPattern.Remove(s)));
        }

        private void UpdateIPLists(IEvlWatcherService service)
        {
            List<IPAddress> ips = new List<IPAddress>(service.GetTemporarilyBannedIPs());
            List<IPAddress> toAdd = new List<IPAddress>();
            List<IPAddress> toRemove = new List<IPAddress>();

            foreach (IPAddress i in ips)
            {
                if (!_temporarilyBannedIps.Contains(i))
                    toAdd.Add(i);
            }
            foreach (IPAddress i in _temporarilyBannedIps)
            {
                if (!ips.Contains(i))
                    toRemove.Add(i);
            }
            foreach (IPAddress i in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => _temporarilyBannedIps.Add(i)));

            foreach (IPAddress i in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => _temporarilyBannedIps.Remove(i)));

            ips = new List<IPAddress>(service.GetPermanentlyBannedIPs());
            toAdd = new List<IPAddress>();
            toRemove = new List<IPAddress>();
            foreach (IPAddress i in ips)
            {
                if (!_permanentlyBannedIps.Contains(i))
                    toAdd.Add(i);
            }
            foreach (IPAddress i in _permanentlyBannedIps)
            {
                if (!ips.Contains(i))
                    toRemove.Add(i);
            }
            foreach (IPAddress i in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => _permanentlyBannedIps.Add(i)));

            foreach (IPAddress i in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => _permanentlyBannedIps.Remove(i)));
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

        public string ConsoleLevel
        {
            get
            {
                return "Not implemented.";
            }
            set
            {
                
                //_consoleLevel = Enum.Parse(typeof(SeverityType value;
                Notify("ConsoleLevel");
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
                Notify("WhiteListFilter");
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
                Notify("PermaBanIPString");
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
                    Notify("PermaBanCount");
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
                    Notify("TriggerCount");
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
                    Notify("TimeFrame");
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
                    Notify("LockTime");
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
                Notify("IsRunning");
            }
        }

        public ObservableCollection<IPAddress> TemporarilyBannedIPs
        {
            get
            {
                return _temporarilyBannedIps;
            }
        }
        public ObservableCollection<IPAddress> PermanentlyBannedIPs
        {
            get
            {
                return _permanentlyBannedIps;
            }
        }
        public ObservableCollection<string> WhiteListedIPs
        {
            get
            {
                return _whiteListPattern;
            }
        }

        public string ConsoleText
        {
            get
            {
                return _consoleText;
            }
            set
            {
                _consoleText = value;
                Notify("ConsoleText");
            }
        }

        #endregion
    }
}
