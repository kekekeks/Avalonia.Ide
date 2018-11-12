using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Editor
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private string _currentAssemblyName;
        private string _text;
        private CompletionSet _completionSet;
        CompletionEngine _engine = new CompletionEngine();
        public Metadata Metadata { get; }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }

        public CompletionSet CompletionSet
        {
            get { return _completionSet; }
            set
            {
                _completionSet = value;
                OnPropertyChanged();
            }
        }

        public void UpdateCompletions(int position)
        {
            CompletionSet = _engine.GetCompletions(Metadata, Text, position, _currentAssemblyName);
        }

        public MainWindowModel(Metadata metadata, string text, string currentAssemblyName)
        {
            _currentAssemblyName = currentAssemblyName;
            Metadata = metadata;
            Text = text ??
                   "<UserControl xmlns='https://github.com/avaloniaui' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>\r\n    <Button></Button>\r\n</UserControl>"
                       .Replace("'", "\"");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
