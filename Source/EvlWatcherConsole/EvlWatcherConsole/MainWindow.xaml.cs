using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using EvlWatcher.Logging;

namespace EvlWatcherConsole
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void ConsoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoscrollCheckBox.IsChecked == true)
                ConsoleTextbox.Focus();
                ConsoleTextbox.CaretIndex = ConsoleTextbox.Text.Length;
                ConsoleTextbox.ScrollToEnd();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            ComboVerbosity.Items.Add(SeverityLevel.Off);
            ComboVerbosity.Items.Add(SeverityLevel.Critical);
            ComboVerbosity.Items.Add(SeverityLevel.Debug);
            ComboVerbosity.Items.Add(SeverityLevel.Error);
            ComboVerbosity.Items.Add(SeverityLevel.Info);
            ComboVerbosity.Items.Add(SeverityLevel.Verbose);
            ComboVerbosity.Items.Add(SeverityLevel.Warning);
            ComboVerbosity.SelectedIndex = 0;
        }
    }
}
