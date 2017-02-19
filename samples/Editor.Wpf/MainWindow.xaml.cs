using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Avalonia.Ide.CompletionEngine;
using AvaloniaVS.IntelliSense;

namespace Editor.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private MainWindowModel Model => (MainWindowModel) DataContext;

        void UpdateCompletionList()
        {
            Model.UpdateCompletions(TextBox.CaretIndex);
            if (Model.CompletionSet?.Completions?.Count > 0)
                ListBox.SelectedIndex = 0;
        }

        void HideCompletionList()
        {
            Model.CompletionSet = null;
        }

        private void OnTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length != 1)
                return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (CompletionEngine.ShouldTriggerCompletionListOn(e.Text[0]))
                    UpdateCompletionList();
                else
                    HideCompletionList();
            }));
        }


        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                e.Handled = true;
                UpdateCompletionList();
            }
            else if (Model.CompletionSet?.Completions?.Count > 0)
            {
                if (e.Key == Key.Down)
                {
                    var nextIndex = ListBox.SelectedIndex + 1;
                    nextIndex = ListBox.Items.Count == nextIndex ? 0 : nextIndex;
                    ListBox.SelectedIndex = nextIndex;
                    ListBox.ScrollIntoView(ListBox.SelectedItems[0]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    var nextIndex = ListBox.SelectedIndex - 1;
                    nextIndex = nextIndex >= 0 ? nextIndex : ListBox.Items.Count - 1;
                    ListBox.SelectedIndex = nextIndex;
                    ListBox.ScrollIntoView(ListBox.SelectedItems[0]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Return)
                {
                    var completion = (Completion)ListBox.SelectedItems[0];
                    var curStart = TextBox.CaretIndex;
                    TextBox.SelectionStart = Model.CompletionSet.StartPosition;
                    TextBox.SelectionLength = curStart - TextBox.SelectionStart;
                    TextBox.SelectedText = completion.InsertText;

                    TextBox.SelectionLength = 0;
                    TextBox.SelectionStart = TextBox.SelectionStart + completion.InsertText.Length;

                    Dispatcher.BeginInvoke(new Action(UpdateCompletionList));

                    e.Handled = true;
                }
                else
                    Model.CompletionSet = null;
            }
            
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            Model.CompletionSet = null;
        }
    }
}
