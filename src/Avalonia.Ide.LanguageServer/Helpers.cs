using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol.Viewport;
using Hjg.Pngcs;

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
                if (t.IsCompletedSuccessfully)
                {
                    lock (tcs)
                    {
                        if (timedOut.IsCancellationRequested)
                            t.Result.Close();
                        else
                            tcs.SetResult(t.Result);
                    }
                }
            });
            if (!tcs.Task.Wait(timeout.Value))
            {
                lock (tcs)
                {
                    if (!tcs.Task.IsCompletedSuccessfully)
                    {
                        timedOut.Cancel();
                        throw new TimeoutException();
                    }
                }
            }
            return tcs.Task.Result;
        }
    }

    public class OneShotTcpServer : IDisposable
    {
        TcpListener _l = new TcpListener(IPAddress.Loopback, 0);

        public OneShotTcpServer()
        {
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