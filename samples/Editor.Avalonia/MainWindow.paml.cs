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
        private CustomCompletionWindow _completionWindow;

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _editor = this.FindControl<TextEditor>("Editor");
            _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");

            _editor.TextArea.TextEntering +=OnTextEntering;
            _editor.TextArea.TextEntered += OnTextEntered;
            this.DataContextChanged += HandleDataContextChanged;
        }

        class CompletionData : ICompletionData
        {
            private readonly Completion _completion;
            private readonly int _startPosition;

            public CompletionData(Completion completion, int startPosition)
            {
                _completion = completion;
                _startPosition = startPosition;
            }

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(_startPosition, completionSegment.EndOffset - _startPosition,
                    _completion.InsertText);
                if (_completion.RecommendedCursorOffset.HasValue)
                    textArea.Caret.Offset = _startPosition + _completion.RecommendedCursorOffset.Value;
                else
                    textArea.Caret.Offset = _startPosition + _completion.InsertText.Length;
            }

            public IBitmap Image => null;
            public string Text => _completion.DisplayText;

            public object Content => _completion.DisplayText;

            public object Description => _completion.Description;

            public double Priority => 1;
        }

        class CustomCompletionWindow : CompletionWindow
        {
            public event Action ReallyClosed;
            public CustomCompletionWindow(TextArea textArea) : base(textArea)
            {
            }

            protected override void OnClosed()
            {
                base.OnClosed();
                ReallyClosed?.Invoke();
            }
        }

        private void OnTextEntered(object sender, TextInputEventArgs e)
        {
            if(_completionWindow != null)
                return;
            if(!BuildCompletionData())
                return;

            // Open code completion after the user has pressed dot:
            _completionWindow = new CustomCompletionWindow(_editor.TextArea);
            AugmentCompletionData();
            _completionWindow.Show();
            _completionWindow.ReallyClosed += delegate
            {
                _completionWindow = null;
            };
        }

        bool BuildCompletionData()
        {
            Model.Text = _editor.Text;
            Model.UpdateCompletions(_editor.SelectionStart);
            return Model.CompletionSet != null && Model.CompletionSet.Completions.Count > 0;
        }

        void AugmentCompletionData()
        {
            IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;
            data.Clear();
            Model.Text = _editor.Text;
            Model.UpdateCompletions(_editor.SelectionStart);
            foreach (var c in Model.CompletionSet.Completions)
                data.Add(new CompletionData(c, Model.CompletionSet.StartPosition));
            _completionWindow.StartOffset = Model.CompletionSet.StartPosition;
        }

        
        private void OnTextEntering(object sender, TextInputEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                
                var typedChar = e.Text[0];
                if (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar))
                    _completionWindow.CompletionList.RequestInsertion(e);
                else
                    AugmentCompletionData();
            }
        }

        void HandleDataContextChanged(object sender, EventArgs eventArgs)
        {
            _editor.Text = Model?.Text ?? "";
        }

        private MainWindowModel Model => (MainWindowModel)DataContext;

        void UpdateCompletionList()
        {
            /*
            Model.UpdateCompletions(_textBox.CaretIndex);
            if (Model.CompletionSet?.Completions?.Count > 0)
                _listBox.SelectedIndex = 0;*/
        }

    }
}
