using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Net;
using System.Windows.Input;
using EvlWatcherConsole.MVVMBase;
using EvlWatcherConsole.Model;
using System.Threading;
using EvlWatcher.WCF.DTO;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
        private SeverityLevelDTO _selectedConsoleLevel = SeverityLevelDTO.Info;

        private bool _isInRuleEditMode;
        private bool _isInGlobalEditMode;

        //global config
        private SeverityLevelDTO _loglevel;
        private int _consoleBackLog;
        private int _checkInterval;
        private IList<string> _availableTaskNames;
        private string _selectedTaskName;
        private string _currentTaskRegex;
        private string _currentTaskEventPath;
        private string _currentTaskBoosters;
        private string _currentTestRegex;
        private string _xmltext;

        #endregion

        #region public .ctor

        public MainWindowViewModel()
        {
            TemporarilyBannedIPs.CollectionChanged += TemporarilyBannedIPs_CollectionChanged;
            _model = new EvlWatcherModel();

            StartUpdating();
        }

        private void TemporarilyBannedIPs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Notify(nameof(CanAddTemporaryToPerma));
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
                        if ((IsGlobalConfigSelected && !IsInGlobalEditMode) || (IsRuleEditorTabSelected && !IsInRuleEditMode))
                        {
                            UpdateGlobalConfig();
                        }
                    }
                }
                catch (Exception)
                {
                    //dont do anything.
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

        private void UpdateGlobalConfig()
        {
            var globalConfig = _model.GetGlobalConfig();
            LogLevel = globalConfig.LogLevel;
            CheckInterval = globalConfig.EventLogInterval;
            ConsoleBackLog = globalConfig.ConsoleBackLog;
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

        public string SelectedTaskName
        {
            get
            {
                return _selectedTaskName;
            }
            set
            {
                _selectedTaskName = value;
                try
                {
                    var config = _model.GetGlobalConfig().GenericTaskConfigurations.Where(t => t.TaskName == value).FirstOrDefault();
                    CurrentTaskRegex = config.Regex;
                    CurrentTaskEventPath = config.EventPath.Aggregate((x, y) => x + ", " + y);
                    CurrentTaskBoosters = config.RegexBoosters.Aggregate((x, y) => x + ", " + y);
                    CurrentTestRegex = CurrentTaskRegex;
                }
                catch
                {
                    _selectedTaskName = null;
                }
            }
        }

        public bool CanAddTemporaryToPerma => IsServiceResponding && TemporarilyBannedIPs.Count > 0;

        public string CurrentTaskRegex
        {
            get
            {
                return _currentTaskRegex;
            }
            set
            {
                _currentTaskRegex = value;
                Notify(nameof(CurrentTaskRegex));
            }
        }

        public string CurrentTestRegex
        {
            get
            {
                return _currentTestRegex;
            }
            set
            {
                _currentTestRegex = value;
                Notify(nameof(CurrentTestRegex));
            }
        }

        public string CurrentTaskEventPath
        {
            get
            {
                return _currentTaskEventPath;
            }
            set
            {
                _currentTaskEventPath = value;
                Notify(nameof(CurrentTaskEventPath));
            }
        }

        public string CurrentTaskBoosters
        {
            get
            {
                return _currentTaskBoosters;
            }
            set
            {
                _currentTaskBoosters = value;
                Notify(nameof(CurrentTaskBoosters));
            }
        }

        public bool IsInRuleEditMode
        {
            get
            {
                return _isInRuleEditMode;
            }
            set
            {
                _isInRuleEditMode = value;
                Notify(nameof(IsInRuleEditMode));
            }
        }
        public bool IsInGlobalEditMode
        {
            get
            {
                return _isInGlobalEditMode;
            }
            set
            {
                _isInGlobalEditMode = value;
                Notify(nameof(IsInGlobalEditMode));
            }
        }

        public IReadOnlyList<SeverityLevelDTO> AvailableLogLevels
        {
            get
            {
                return _model.ConsoleLevels;
            }
        }

        public ICommand SaveGlobalConfigCommand
        {
            get
            {
                return new RelayCommand(p => { SaveConfiguration(); }, p => IsInGlobalEditMode && IsServiceResponding);
            }
        }

        public ICommand CancelGlobalEditingCommand
        {
            get
            {
                return new RelayCommand(p => { IsInGlobalEditMode = false; }, p => IsInGlobalEditMode);
            }
        }

        public ICommand TestRegexCommand
        {
            get
            {
                return new RelayCommand(p => { TestRegex(); }, p => true);
            }
        }

        public string XMLText
        {
            get
            {
                return _xmltext;
            }
            set
            {
                _xmltext = value;
                Notify(nameof(XMLText));
            }
        }

        private void TestRegex()
        {
            try
            {
                var regex = new Regex(CurrentTestRegex, RegexOptions.Compiled);
                var match = regex.Match(XMLText);
                if (match.Success && match.Groups.Count == 2 && IPAddress.TryParse(match.Groups[1].Value, out IPAddress ipAddress))
                {
                    MessageBox.Show($"We could extract the IP {ipAddress} from that.", "Success", MessageBoxButton.OK, MessageBoxImage.None);
                }
                else
                {
                    MessageBox.Show("We couldnt extract an IP from that.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("We couldnt test that.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ICommand ResetRegexCommand
        {
            get
            {
                return new RelayCommand(p => { CurrentTestRegex = CurrentTaskRegex; }, p => true);
            }
        }

        public ICommand ToggleGlobalConfigEditModeCommand
        {
            get
            {
                return new RelayCommand(p => { IsInGlobalEditMode = true; }, p => !IsInGlobalEditMode);
            }
        }

        private void SaveConfiguration()
        {
            _model.SaveGlobalConfig(LogLevel, ConsoleBackLog, CheckInterval);
            IsInGlobalEditMode = false;
        }

        public ICommand MoveTemporaryToPermaCommand
        {
            get
            {
                return new RelayCommand(p => { _model.AddPermanentIPBan(SelectedTemporaryIP); }, p => { return SelectedTemporaryIP != null; });
            }
        }
        public ICommand MoveAllTemporaryToPermaCommand
        {
            get
            {
                return new RelayCommand(p => { _model.AddPermanentIPBans(TemporarilyBannedIPs.ToArray()); }, p => { return CanAddTemporaryToPerma; });
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
        public bool IsGlobalConfigSelected
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

        public SeverityLevelDTO SelectedConsoleLevel
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


        public IList<string> AvailableTaskNames
        {
            get
            {
                if(_availableTaskNames == null)
                    _availableTaskNames = _model.GetGlobalConfig().GenericTaskConfigurations.Select(p => p.TaskName).ToList();
                return _availableTaskNames;
            }
        }

        
        public ICommand RemoveTemporaryBanCommand
        {
            get
            {
                return new RelayCommand(p =>
                { _model.RemoveTemporaryBan(SelectedTemporaryIP); }, p => SelectedTemporaryIP != null && IsServiceResponding );
            }
        }

        public ICommand AddPermaBanCommand
        {
            get
            {
                return new RelayCommand(p =>
                   { _model.AddPermanentIPBan(IPAddress.Parse(PermaBanIPString)); PermaBanIPString = ""; }, p => { return IPAddress.TryParse(PermaBanIPString, out IPAddress dummy) && IsServiceResponding; });
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
                Notify(nameof(CanAddTemporaryToPerma));
            }
        }

        public ObservableCollection<IPAddress> TemporarilyBannedIPs { get; } = new ObservableCollection<IPAddress>();
        public ObservableCollection<IPAddress> PermanentlyBannedIPs { get; } = new ObservableCollection<IPAddress>();
        public ObservableCollection<string> WhiteListedPatterns { get; } = new ObservableCollection<string>();

        public int CheckInterval
        {
            get
            {
                return _checkInterval;
            }
            set
            {
                _checkInterval = value;
                Notify(nameof(CheckInterval));
            }
        }

        public int ConsoleBackLog
        {
            get
            {
                return _consoleBackLog;
            }
            set
            {
                _consoleBackLog = value;
                Notify(nameof(ConsoleBackLog));
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
                Notify(nameof(ConsoleText));
            }
        }

        public ObservableCollection<LogEntryDTO> ConsoleHistory { get; } = new ObservableCollection<LogEntryDTO>();

        public SeverityLevelDTO LogLevel
        {
            get
            {
                return _loglevel;
            }
            set
            {
                _loglevel = value;
                Notify(nameof(LogLevel));
            }
        }
     
        #endregion
    }
}
