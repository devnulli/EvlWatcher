using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Net;
using System.Windows.Input;
using EvlWatcherConsole.MVVMBase;
using EvlWatcher.WCF;
using EvlWatcher.Logging;
using EvlWatcherConsole.Model;
using System.Threading;
using System.Runtime.CompilerServices;

namespace EvlWatcherConsole.ViewModel
{
    public class MainWindowViewModel : GuiObject
    {
        #region private members

        private Thread _updater = null;
        private static volatile bool _run = true;

        private readonly EvlWatcherModel _model;

        private bool _isServiceResponding = false;
        private string _permaBanIPString = "";
        private string _whiteListFilter = "";
        private string _consoleText;
        private SeverityLevel _selectedConsoleLevel = SeverityLevel.Info;

        #endregion

        #region public .ctor

        public MainWindowViewModel()
        {
            _model = new EvlWatcherModel();

            StartUpdating();
        }

        ~MainWindowViewModel()
        {
            StopUpdating();
        }

        #endregion

        #region private operations

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

        private void StartUpdating()
        {
            _updater = new Thread(new ThreadStart(this.Run))
            {
                IsBackground = true
            };

            _run = true;
            _updater.Start();
        }


        private void Run()
        {
            while (_run)
            {
                bool serviceResponding = false;

                try
                {
                    serviceResponding = _model.IsServiceResponding;

                    if (serviceResponding)
                    {
                        if (IsIPTabSelected)
                        {
                            UpdateIPLists();
                            UpdateWhileListPattern();
                        }
                        if (IsConsoleTabSelected)
                        {
                            UpdateConsole();
                        }

                    }
                }
                catch(Exception ex)
                {
                    
                }
                finally
                {
                    IsServiceResponding = serviceResponding;
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

        private void UpdateConsole()
        {
            var data = _model.GetConsoleHistory(SelectedConsoleLevel);
         
            var sb = new StringBuilder();

            foreach (var log in data)
                sb.AppendLine($"{log.Date} - [{log.Severity}]: {log.Message}");

            ConsoleText = sb.ToString();
        }

        private void UpdateWhileListPattern()
        {
            var whiteListEntries = _model.GetWhiteListPatterns();
            List<string> toAdd = whiteListEntries.Where(IP => !WhiteListedPatterns.Contains(IP)).ToList();
            List<string> toRemove = WhiteListedPatterns.Where(IP => !whiteListEntries.Contains(IP)).ToList();

         
            foreach (string s in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => WhiteListedPatterns.Add(s)));

            foreach (string s in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => WhiteListedPatterns.Remove(s)));
        }

        private void UpdateIPLists()
        {
            var currentlyBannedIPs = _model.GetTemporarilyBannedIPs();

            List<IPAddress> toAdd = currentlyBannedIPs.Where(IP => !TemporarilyBannedIPs.Contains(IP)).ToList();
            List<IPAddress> toRemove = TemporarilyBannedIPs.Where(IP => !currentlyBannedIPs.Contains(IP)).ToList();

            foreach (IPAddress i in toAdd)
                Application.Current.Dispatcher.Invoke(new Action(() => TemporarilyBannedIPs.Add(i)));

            foreach (IPAddress i in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => TemporarilyBannedIPs.Remove(i)));

            var permanentlyBannedIPs = _model.GetPermanentlyBannedIPs();

            toAdd = permanentlyBannedIPs.Where(IP => !PermanentlyBannedIPs.Contains(IP)).ToList();
            toRemove = PermanentlyBannedIPs.Where(IP => !permanentlyBannedIPs.Contains(IP)).ToList();
            
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
                return new RelayCommand(p => { _model.AddPermanentIPBan(SelectedTemporaryIP); }, p => { return SelectedTemporaryIP != null; });
            }
        }

        public bool IsIPTabSelected
        {
            get; set;
        }

        public bool IsConsoleTabSelected
        {
            get; set;
        }

        public bool IsRuleEditorTabSelected
        {
            get; set;
        }

        public ICommand MoveTemporaryToWhiteListCommand
        {
            get
            {
                return new RelayCommand(p => { _model.AddWhiteListEntry(SelectedTemporaryIP.ToString()); }, p => { return SelectedTemporaryIP != null; });
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
                return new RelayCommand(p =>
                   { _model.AddPermanentIPBan(IPAddress.Parse(PermaBanIPString)); PermaBanIPString = ""; }, p => { IPAddress dummy; return IPAddress.TryParse(PermaBanIPString, out dummy) && IsServiceResponding; });
            }
        }

        public ICommand RemovePermaBanCommand
        {
            get
            {
                return new RelayCommand(p => _model.RemovePermanentIPBan(SelectedPermanentIP), p => { return SelectedPermanentIP != null && IsServiceResponding; });
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
                return new RelayCommand(p => { _model.AddWhiteListEntry(WhiteListFilter); WhiteListFilter = ""; }, p => { return IsServiceResponding; });
            }
        }

        public ICommand RemoveWhiteListFilterCommand
        {
            get
            {
                return new RelayCommand(p => { _model.RemoveWhiteListEntry(SelectedWhiteListPattern); }, p => { return SelectedWhiteListPattern != null && IsServiceResponding; });
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

        public bool IsServiceResponding
        {
            get
            {
                return _isServiceResponding;
            }

            private set
            {
                _isServiceResponding = value;
                Notify(nameof(IsServiceResponding));
            }
        }

        public ObservableCollection<IPAddress> TemporarilyBannedIPs { get; } = new ObservableCollection<IPAddress>();
        public ObservableCollection<IPAddress> PermanentlyBannedIPs { get; } = new ObservableCollection<IPAddress>();
        public ObservableCollection<string> WhiteListedPatterns { get; } = new ObservableCollection<string>();

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
