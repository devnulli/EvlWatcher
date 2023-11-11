using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void ListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control && sender is ListView)
            {
                var list = sender as ListView;

                StringBuilder b = new StringBuilder();
                foreach (var listItem in list.SelectedItems)
                {
                    b.AppendLine(listItem.ToString());
                }
                Clipboard.SetText(b.ToString());
            }
        }
    }
}
