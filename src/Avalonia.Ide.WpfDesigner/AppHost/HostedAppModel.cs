using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Avalonia.Designer.Comm;
using Avalonia.Remote.Protocol.Designer;

namespace Avalonia.Designer.AppHost
{
    public class HostedAppModel : INotifyPropertyChanged
    {
        private readonly ProcessHost _host;
        private IntPtr _nativeWindowHandle;
        private string _error;
        private string _errorDetails;

        internal HostedAppModel(ProcessHost host)
        {
            _host = host;
            Background = Settings.Background;
            host.OnMessage += OnMessage;
        }

        private void OnMessage(object obj)
        {
            if (obj is UpdateXamlResultMessage res)
            {
                NativeWindowHandle = _host.WindowHandle;
                SetError(res.Error != null ? "Error" : null, res.Error);
            }
        }

        public IntPtr NativeWindowHandle
        {
            get { return _nativeWindowHandle; }
            set
            {
                if (value.Equals(_nativeWindowHandle)) return;
                _nativeWindowHandle = value;
                OnPropertyChanged();
            }
        }

        public string Error
        {
            get { return _error; }
            private set
            {
                if (value == _error) return;
                _error = value;
                OnPropertyChanged();
            }
        }

        public string ErrorDetails
        {
            get { return _errorDetails; }
            private set
            {
                if (value == _errorDetails) return;
                _errorDetails = value;
                OnPropertyChanged();
            }
        }

        public string Background
        {
            get { return _background; }
            set
            {
                if (value == _background) return;
                _background = value;
                OnPropertyChanged();
            }
        }

        public void SetError(string error, string details = null)
        {
            Error = error;
            ErrorDetails = details;
        }
        
        private string _background;

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
