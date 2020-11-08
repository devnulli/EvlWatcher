using System.ComponentModel;

namespace EvlWatcherConsole.MVVMBase
{
    public class GuiObject : INotifyPropertyChanged
    {
        #region public events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        protected void Notify(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
