using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Avalonia.Designer.Comm;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;

namespace Avalonia.Designer.AppHost
{
    static class DesignerTcpListener
    {
        private static Dictionary<string, WeakReference<ProcessHost>> s_registered =
            new Dictionary<string, WeakReference<ProcessHost>>();

        public static int Port { get; private set; } = -1;

        public static void Register(ProcessHost host, string sessionId)
        {
            lock (s_registered)
            {
                s_registered[sessionId] = new WeakReference<ProcessHost>(host);
                if (Port == -1)
                {
                    var tcpListener = new TcpListener(IPAddress.Loopback, 0);
                    tcpListener.Start();
                    Port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
                    tcpListener.Stop();
                    new BsonTcpTransport().Listen(IPAddress.Loopback, Port, conn =>
                    {
                        conn.OnMessage += (_, msg) =>
                        {
                            if (msg is StartDesignerSessionMessage start)
                            {
                                lock (s_registered)
                                {
                                    if (s_registered.TryGetValue(start.SessionId, out var hostref)
                                        && hostref.TryGetTarget(out var found))
                                        found.OnSessionStarted(conn);
                                    else
                                        conn.Dispose();
                                }
                            }
                        };
                    });
                }
            }
        }

    }
}
