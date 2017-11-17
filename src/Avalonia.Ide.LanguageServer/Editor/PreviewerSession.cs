using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Viewport;
using Hjg.Pngcs;

namespace Avalonia.Ide.LanguageServer.Editor
{
    public class PreviewerSession : IDisposable
    {
        private IAvaloniaRemoteTransportConnection _transport;
        private Action _onDispose;
        private FrameMessage _lastFrame;
        private long _lastConsumedFrame = -1;
        private object _lock = new object();
        public PreviewerSession(IAvaloniaRemoteTransportConnection transport, Action onDispose)
        {
            _transport = transport;
            _onDispose = onDispose;
            _transport.OnException += OnTransportException;
            _transport.OnMessage += TransportOnOnMessage;
            _transport.Send(new ClientSupportedPixelFormatsMessage
                {Formats = new[] {PixelFormat.Rgba8888}}).Wait();
            _transport.Send(new ClientViewportAllocatedMessage
            {
                DpiX = 96,
                DpiY = 96,
                Width = 1,
                Height = 1
            }).Wait();
        }

        private void OnTransportException(IAvaloniaRemoteTransportConnection conn, Exception e)
        {
            Log.Message("Transport exception:" + e);
        }

        private void TransportOnOnMessage(IAvaloniaRemoteTransportConnection conn, object msg)
        {
            if (msg is FrameMessage frame)
            {
                lock (_lock)
                    _lastFrame = frame;
            }
        }

        public long? CurrentSequenceId
        {
            get
            {
                lock (_lock)
                    return _lastFrame?.SequenceId;
            }
        }
        
        public FrameMessage ConsumeFrame(long lastKnownFrameId)
        {
            lock (_lock)
            {

                if (_lastFrame != null && lastKnownFrameId > _lastConsumedFrame)
                {
                    _transport.Send(new FrameReceivedMessage
                    {
                        SequenceId = _lastFrame.SequenceId
                    });
                    _lastConsumedFrame = _lastFrame.SequenceId;
                }
                return _lastFrame;
            }
        }

        public void UpdateXaml(string xaml, string assemblyPath)
        {
            _transport.Send(new UpdateXamlMessage
            {
                Xaml = xaml,
                AssemblyPath = assemblyPath
            });
        }
        
        public void OnTransportDisconnected()
        {
            Dispose();
        }

        public bool IsAlive => _transport != null;
        
        public void Dispose()
        {
            var d = _onDispose;
            _onDispose = null;
            d?.Invoke();
            try
            {
                var t = _transport;
                _transport = null;
                t?.Dispose();
            }
            catch
            {
                //
            }
        }
    }
    
    public static class PreviewerSessionConnector
    {

        private const string PreviewerDllName = "Avalonia.Designer.HostApp.dll";
        private const string PreviewerPackagePath = "../../tools/netcoreapp2.0/previewer/" + PreviewerDllName;
        
        public static (PreviewerSession session, string error) Start(string targetAssembly)
        {
            targetAssembly =
                "/home/kekekeks/Projects/AvaloniaMaster/samples/ControlCatalog.NetCore/bin/Debug/netcoreapp2.0/ControlCatalog.NetCore.dll";
            string targetDir = Path.GetDirectoryName(targetAssembly);
            string targetBasePath = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(targetAssembly));
            var previewerPath = Path.Combine(targetDir, PreviewerDllName);
            if (!File.Exists(previewerPath))
            {
                previewerPath = null;
                var depsJsonPath = Path.Combine(targetDir,
                    Path.GetFileNameWithoutExtension(targetAssembly) + ".deps.json");
                if (File.Exists(depsJsonPath))
                {
                    var avaloniaDll = DepsJsonAssemblyListLoader.ParseFile(depsJsonPath)
                        .FirstOrDefault(x => x.EndsWith("Avalonia.dll"));
                    if (avaloniaDll != null)
                    {
                        previewerPath = Path.Combine(Path.GetDirectoryName(avaloniaDll), PreviewerPackagePath);
                        if (!File.Exists(previewerPath))
                            previewerPath = null;
                    }
                }
            }
            if (previewerPath == null)
                return (null, "Unable to locate Avalonia.Designer.HostApp.dll");

            Process proc = null;
            try
            {
                using (var l = new OneShotTcpServer())
                {
                    
                    var cmdline =
                        $@"exec --runtimeconfig {targetBasePath}.runtimeconfig.json --depsfile {
                                targetBasePath
                            }.deps.json {previewerPath} --transport tcp-bson://127.0.0.1:{l.Port}/ {targetAssembly}";
                    
                    proc = Process.Start(new ProcessStartInfo("dotnet", cmdline)
                    {
                        UseShellExecute = false
                    });

                    var client = l.WaitForOneConnection(new TimeSpan(0, 0, 30));
                    PreviewerSession session = null;
                    var transport = BsonTransportHack.CreateBsonTransport(client.GetStream(), () =>
                    {
                        client.Dispose();
                        session?.OnTransportDisconnected();
                    });
                    session = new PreviewerSession(transport, () =>
                    {
                        try
                        {
                            proc.Kill();
                        }
                        catch
                        {
                        }
                    });
                    return (session, null);
                }
            }
            catch (Exception e)
            {
                try
                {
                    proc?.Kill();
                }
                catch
                {
                    //Ignore
                }
                proc?.Dispose();
                return (null, "Unable to establish previewer transport connection: " + e);
            }

            return (null, null);
        }

        class BsonTransportHack : TcpTransportBase
        {
            
            public BsonTransportHack(IMessageTypeResolver resolver) : base(resolver)
            {
            }
            
            //HACK: Change code below if this one will stop compiling
            protected override IAvaloniaRemoteTransportConnection
                CreateTransport(IMessageTypeResolver resolver, Stream stream, Action disposeCallback)
            {
                throw new NotSupportedException();
            }

            public static IAvaloniaRemoteTransportConnection
                CreateBsonTransport(Stream stream, Action disposeCallback)
                => (IAvaloniaRemoteTransportConnection) typeof(BsonTcpTransport).GetMethod("CreateTransport",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Invoke(new BsonTcpTransport(),
                        new object[]
                        {
                            new DefaultMessageTypeResolver(typeof(BsonTcpTransport).GetTypeInfo().Assembly),
                            stream,
                            disposeCallback
                        });
        }
    }
}