using System.Windows;
using System.Windows.Controls;

namespace EvlWatcherConsole.View
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void ConsoleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoscrollCheckBox.IsChecked == true)
            {
                ConsoleTextbox.Focus();
                ConsoleTextbox.CaretIndex = ConsoleTextbox.Text.Length;
                ConsoleTextbox.ScrollToEnd();
            }
        }
    }
}
