using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using Avalonia.Designer.AppHost;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;

namespace Avalonia.Designer.Comm
{
    class ProcessHost : INotifyPropertyChanged
    {
        public event Action<object> OnMessage;
        public event Action<Process> SpawnedProcess;
        private readonly DesignerConfiguration _config;
        private IAvaloniaRemoteTransportConnection _conn;
        private string _state;
        public string State
        {
            get { return _state; }
            set
            {
                if (_state == value)
                    return;
                _state = value;
                OnPropertyChanged();
            }
        }

        private bool _isAlive;
        private readonly SynchronizationContext _dispatcher;
        private Process _proc;

        public bool IsAlive
        {
            get { return _isAlive; }
            set
            {
                if (_isAlive == value)
                    return;
                _isAlive = value;
                OnPropertyChanged();
            }
        }

        private IntPtr _windowHandle;
        private UpdateXamlMessage _xaml;

        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
            set
            {
                if (_windowHandle == value)
                    return;
                _windowHandle = value;
                OnPropertyChanged();
            }
        }

        public ProcessHost(DesignerConfiguration config)
        {
            _config = config;
            _dispatcher = SynchronizationContext.Current;
        }

        void OnExited(object sender, EventArgs eventArgs)
        {
            if (_proc != sender)
                return;
            _conn?.Dispose();
            _conn = null;
            _proc = null;
            _dispatcher.Post(_ =>
            {
                HandleExited();
            }, null);
        }

        void HandleExited()
        {
            IsAlive = false;
            WindowHandle = IntPtr.Zero;
            State = "Designer process crashed" + Environment.NewLine + State;
        }

        public void Start(string targetExe, string xaml, string sourceAssembly)
        {
            _xaml = new UpdateXamlMessage {Xaml = xaml, AssemblyPath = sourceAssembly};
            if (_proc != null)
            {
                _proc.Exited -= OnExited;
                try
                {
                    _conn?.Dispose();
                    _conn = null;
                    _proc.Kill();
                }
                catch { }
                HandleExited();
                State = "Restarting...";
            }

            var netCore = false;
            var targetDir = Path.GetDirectoryName(targetExe);
            var targetBase = Path.Combine(targetDir,
                Path.GetFileNameWithoutExtension(targetExe));
            
            var depsJsonPath = targetBase + ".deps.json";

            netCore = File.Exists(depsJsonPath) &&
                      DepsJson.Load(depsJsonPath)?.RuntimeTarget?.Name?.Contains("NETCoreApp") == true;
            var sessionId = Guid.NewGuid().ToString();
            DesignerTcpListener.Register(this, sessionId);
            var cmdline =
                $"--transport tcp-bson://127.0.0.1:{DesignerTcpListener.Port}/ --session-id {sessionId} --method win32 \"{targetExe}\"";
            if (netCore)
            {
                cmdline =
                    $"exec --runtimeconfig \"{targetBase}.runtimeconfig.json\" --depsfile \"{depsJsonPath}\" \"{_config.NetCoreAppHostPath}\" " +
                    cmdline;
            }
            var exe = netCore ? "dotnet" : _config.NetFxAppHostPath;
            _proc = new Process()
            {
                StartInfo = new ProcessStartInfo(exe, cmdline)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = targetDir
                },
                EnableRaisingEvents = true
            };

            _proc.Exited += OnExited;
            try
            {
                _proc.Start();
                SpawnedProcess?.Invoke(_proc);
                State = "Launching designer process: " + Environment.NewLine
                        + exe + " " + cmdline + Environment.NewLine + "from directory " + targetDir;
                StartReaders(_proc);
            }
            catch (Exception e)
            {
                State = e.ToString();
                HandleExited();
            }
            IsAlive = true;
        }

        void StartReaders(Process proc)
        {
            foreach (var s in new[] { proc.StandardOutput, proc.StandardError })
            {
                Task.Factory.StartNew(async () =>
                    {
                        while (true)
                        {
                            string line;
                            try
                            {
                                line = await s.ReadLineAsync();
                            }
                            catch
                            {
                                return;
                            }
                            if (line == null)
                                return;
                            if (_proc == proc)
                                State += Environment.NewLine + line;
                        }
                    }, CancellationToken.None, TaskCreationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        public void UpdateXaml(string xaml, string sourceAssembly)
        {
            _xaml = new UpdateXamlMessage {AssemblyPath = sourceAssembly, Xaml = xaml};
            _conn?.Send(_xaml);
        }
        
        private void HandleMessage(IAvaloniaRemoteTransportConnection conn, object msg)
        {
            if (msg is UpdateXamlResultMessage res)
            {
                IntPtr h = IntPtr.Zero;
                if (res.Handle != null)
                    h = new IntPtr(long.Parse(res.Handle));
                WindowHandle = h;
                State = res.Error;
            }
            OnMessage?.Invoke(msg);
        }

        public void Kill()
        {
            try
            {
                _proc?.Kill();
                _proc = null;
            }
            catch
            {
                //
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnSessionStarted(IAvaloniaRemoteTransportConnection conn)
        {
            _conn = conn;
            if (_xaml != null)
                _conn.Send(_xaml);
            conn.OnMessage += (c, msg) => _dispatcher.Post(_ =>
            {
                if (c == _conn)
                    HandleMessage(c, msg);
            }, null);
        }
    }
}
