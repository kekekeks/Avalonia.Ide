using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Editor.Avalonia
{
    class MainWindow : Window
    {
        private TextBox _textBox;
        private ListBox _listBox;

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);

            _textBox = this.FindControl<TextBox>("TextBox");
            _listBox = this.FindControl<ListBox>("ListBox");
            _textBox.TextInput += OnTextInput;
            _textBox.AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
            _textBox.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            _textBox.GetObservable(TextBox.TextProperty).Subscribe(TextChanged);
        }

        private MainWindowModel Model => (MainWindowModel)DataContext;

        void UpdateCompletionList()
        {
            Model.UpdateCompletions(_textBox.CaretIndex);
            if (Model.CompletionSet?.Completions?.Count > 0)
                _listBox.SelectedIndex = 0;
        }

        void HideCompletionList()
        {
            Model.CompletionSet = null;
        }

        bool _suppressTextEvent;

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            if (_suppressTextEvent)
            {
                e.Handled = true;
                _suppressTextEvent = false;
            }
            if (e.Text.Length != 1)
                return;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (CompletionEngine.ShouldTriggerCompletionListOn(e.Text[0]))
                    UpdateCompletionList();
                else
                    HideCompletionList();
            });
        }


        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && (e.Modifiers & InputModifiers.Control) != 0)
            {
                e.Handled = true;
                _suppressTextEvent = true;
                UpdateCompletionList();
            }
            else if (Model.CompletionSet?.Completions?.Count > 0)
            {
                var count = Model.CompletionSet.Completions.Count;
                if (e.Key == Key.Down)
                {
                    var nextIndex = _listBox.SelectedIndex + 1;
                    nextIndex = count == nextIndex ? 0 : nextIndex;
                    _listBox.SelectedIndex = nextIndex;
                    _listBox.ScrollIntoView(_listBox.SelectedItems[0]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    var nextIndex = _listBox.SelectedIndex - 1;
                    nextIndex = nextIndex >= 0 ? nextIndex : count - 1;
                    _listBox.SelectedIndex = nextIndex;
                    _listBox.ScrollIntoView(_listBox.SelectedItems[0]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Return)
                {
                    var completion = (Completion)_listBox.SelectedItems[0];
                    var curStart = _textBox.CaretIndex;
                    _textBox.SelectionStart = Model.CompletionSet.StartPosition;

                    _textBox.SelectionEnd = curStart;
                    _textBox.RaiseEvent(new TextInputEventArgs
                    {
                        Text = completion.InsertText,
                        RoutedEvent = TextBox.TextInputEvent,
                        Source = _textBox,
                        Route = RoutingStrategies.Direct
                    });
                    Dispatcher.UIThread.InvokeAsync(UpdateCompletionList);

                    e.Handled = true;
                }
                else
                    Model.CompletionSet = null;
            }

        }

        private void TextChanged(string text)
        {
            if (Model == null)
                return;
            Model.CompletionSet = null;
        }
    }
}
