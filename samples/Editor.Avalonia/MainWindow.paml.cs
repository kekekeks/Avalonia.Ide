using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;

namespace Editor.Avalonia
{
    class MainWindow : Window
    {
        private readonly TextEditor _editor;
        private ListBox _listBox;
        private MainWindowModel Model => (MainWindowModel) DataContext;

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _editor = this.FindControl<TextEditor>("Editor");
            _listBox = this.FindControl<ListBox>("ListBox");
            _editor.TextArea.TextEntering += OnTextEntering;
            _editor.TextArea.TextEntered += OnTextEntered;
            DataContextChanged += HandleDataContextChanged;
        }

        static MainWindow()
        {
            KeyDownEvent.AddClassHandler<MainWindow>(w => w.HandleKeyDown, RoutingStrategies.Tunnel);
        }

        private void HandleDataContextChanged(object sender, EventArgs e) => _editor.TextArea.Document.Text = Model?.Text ?? "";

        private void OnTextEntered(object sender, TextInputEventArgs e)
        {
            if (CompletionEngine.ShouldTriggerCompletionListOn(e.Text[0]))
                UpdateCompletionList();
            else
                HideCompletionList();
        }

        private void OnTextEntering(object sender, TextInputEventArgs e)
        {
            if (e.Text.Length > 0 && Model.CompletionSet != null && Model.CompletionSet.Completions.Count != 0)
            {
                var typedChar = e.Text[0];
                if (char.IsWhiteSpace(typedChar) || (char.IsPunctuation(typedChar) && typedChar != '/'))
                {
                    var completion = Model.CompletionSet.Completions[
                        Math.Min(_listBox.SelectedIndex, Model.CompletionSet.Completions.Count - 1)];
                    Complete(completion);
                }
            }
        }

        void Complete(Completion completion)
        {
            var set = Model.CompletionSet;
            var textArea = _editor.TextArea;
            var startPosition = set.StartPosition;
            textArea.Document.Replace(startPosition, textArea.Caret.Offset - startPosition,
                completion.InsertText);
            if (completion.RecommendedCursorOffset.HasValue)
                textArea.Caret.Offset = startPosition + completion.RecommendedCursorOffset.Value;
            else
                textArea.Caret.Offset = startPosition + completion.InsertText.Length;

        }

        void UpdateCompletionList()
        {
            Model.Text = _editor.TextArea.Document.Text;
            Model.UpdateCompletions(_editor.TextArea.Caret.Offset);
            if (Model.CompletionSet?.Completions?.Count > 0)
                _listBox.SelectedIndex = 0;
        }

        void HideCompletionList()
        {
            Model.CompletionSet = null;
        }
        
        private void HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space && (e.Modifiers & InputModifiers.Control) != 0)
            {
                e.Handled = true;
                UpdateCompletionList();
            }
            else if (Model.CompletionSet?.Completions?.Count > 0)
            {
                var itemCount = Model.CompletionSet.Completions.Count;
                if (e.Key == Key.Down)
                {
                    var nextIndex = _listBox.SelectedIndex + 1;
                    nextIndex = itemCount == nextIndex ? 0 : nextIndex;
                    _listBox.SelectedIndex = nextIndex;
                    _listBox.ScrollIntoView(_listBox.SelectedItems[0]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    var nextIndex = _listBox.SelectedIndex - 1;
                    nextIndex = nextIndex >= 0 ? nextIndex : itemCount - 1;
                    _listBox.SelectedIndex = nextIndex;
                    _listBox.ScrollIntoView(_listBox.SelectedItems[0]);
                    e.Handled = true;
                }
                else if (e.Key == Key.Return)
                {
                    var completion = (Completion)_listBox.SelectedItems[0];
                    Complete(completion);
                    Dispatcher.UIThread.InvokeAsync(new Action(UpdateCompletionList));
                    e.Handled = true;
                }
                else
                    Model.CompletionSet = null;
            }
            
        }
    }
}
