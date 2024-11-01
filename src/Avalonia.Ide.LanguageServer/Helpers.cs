using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol.Viewport;
using Hjg.Pngcs;
using Newtonsoft.Json.Linq;

namespace Avalonia.Ide.LanguageServer
{
    public static class Helpers
    {       
        public static byte[] EncodePng(this FrameMessage frame)
        {
            var ms = new MemoryStream();
            var pngw = new PngWriter(ms, new ImageInfo(frame.Width, frame.Height, 8, true));
            var row = new byte[4 * frame.Width];
            for (int y = 0; y < frame.Height; y++)
            {
                var off = y * frame.Stride;
                Buffer.BlockCopy(frame.Data, off, row, 0, frame.Width * 4);
                pngw.WriteRowByte(row, y);
            }
            pngw.End();
            return ms.ToArray();
        }
        
        public static TcpClient WaitForOneTcpConnection(this TcpListener l, TimeSpan? timeout = null)
        {
            timeout = timeout ?? new TimeSpan(0, 0, 10);
            var timedOut = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<TcpClient>();
            l.AcceptTcpClientAsync().ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    return;
                }

                lock (tcs)
                {
                    if (timedOut.IsCancellationRequested)
                        t.Result.Close();
                    else
                        tcs.SetResult(t.Result);
                }
            });
            if (!tcs.Task.Wait(timeout.Value))
            {
                lock (tcs)
                {
                    if (tcs.Task.IsFaulted || tcs.Task.IsCanceled)
                    {
                        timedOut.Cancel();
                        throw new TimeoutException();
                    }
                }
            }
            return tcs.Task.Result;
        }

        public static async Task<string> ReceiveStringMessage(this WebSocket ws) => Encoding.UTF8.GetString(await ReceiveMessage(ws));

        public static async Task<byte[]> ReceiveMessage(this WebSocket ws)
        {
            var ms = new MemoryStream();
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open)
            {
                var res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if(ws.State != WebSocketState.Open)
                    break;
                ms.Write(buffer, 0, res.Count);
                if (res.EndOfMessage)
                    return ms.ToArray();
            }
            throw new EndOfStreamException();
        }

        public static Task SendJson(this WebSocket ws, object data)
        {
            var bdata = Encoding.UTF8.GetBytes(JObject.FromObject(data).ToString());
            return ws.SendAsync(new ArraySegment<byte>(bdata), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public class OneShotTcpServer : IDisposable
    {
        private TcpListener _l;

        public OneShotTcpServer(int port = 0)
        {
            _l = new TcpListener(IPAddress.Loopback, port);
            _l.Start();
            Port = ((IPEndPoint) _l.LocalEndpoint).Port;
        }

        public int Port { get; }

        public void Dispose()
        {
            try
            {
                _l.Stop();
            }
            catch
            {
                //Ignore
            }
        }

        public TcpClient WaitForOneConnection(TimeSpan? timeout = null) => _l.WaitForOneTcpConnection(timeout);

    }
}