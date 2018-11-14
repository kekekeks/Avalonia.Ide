using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.LanguageServer.MSBuild;
using Avalonia.Ide.LanguageServer.MSBuild.Requests;

namespace Avalonia.Ide.LanguageServer
{
    public class MsBuildHost
    {
        private WireHelper _connection;
        private TcpClient _client;
        private object _lock = new object();
        void EnsureConnection()
        {
            if (_connection == null || !_client.Connected)
            {
                _client?.Dispose();
                using(var l = new OneShotTcpServer())
                {
                    var path = typeof(NextRequestType).Assembly.GetModules()[0].FullyQualifiedName;
                    path = Path.Combine(Path.GetDirectoryName(path), "host.csproj");
                    Process.Start("dotnet", $"msbuild /p:AvaloniaIdePort={l.Port} {path}");
                    _client = l.WaitForOneConnection();
                    _connection = new WireHelper(_client.GetStream());
                }              
            }
        }

        public TRes SendRequest<TRes>(RequestBase<TRes> req)
        {
            lock (_lock)
            {
                EnsureConnection();
                _connection.SendRequest(req);
                var e = _connection.Read<ResponseEnvelope<TRes>>();
                if (e.Exception != null)
                    throw new TargetInvocationException(e.Exception, null);
                return e.Response;
            }
        }
    }
}