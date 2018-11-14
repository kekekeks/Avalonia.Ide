using System;
using System.Globalization;
using System.Net.WebSockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.LanguageServer.Editor;
using Microsoft.AspNetCore.Http;

namespace Avalonia.Ide.LanguageServer.Web
{
    public class PreviewerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly EditorSessionManager _mgr;

        public PreviewerMiddleware(RequestDelegate next, EditorSessionManager mgr)
        {
            _next = next;
            _mgr = mgr;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == new PathString("/previewer"))
            {
                var ws = await context.WebSockets.AcceptWebSocketAsync();
                await Start(ws);
            }
            await _next(context);
        }
        
        async Task Start(WebSocket ws)
        {
            var path = await ws.ReceiveStringMessage();
            using (var sessionRef = _mgr.GetSession(path))
            {
                var session = sessionRef.Item;
                PreviewerSession previewer = null;
                long lastKnownFrame = -1;
                while (ws.State == WebSocketState.Open)
                {
                    var previewerSession = session.GetPreviewerSession();
                    if (previewerSession.session != null)
                    {
                        if (previewer != previewerSession.session)
                            lastKnownFrame = -1;
                        previewer = previewerSession.session;
                        var frame = previewer.ConsumeFrame(lastKnownFrame);
                        if (frame != null)
                        {
                            lastKnownFrame = frame.SequenceId;
                            var png = frame.EncodePng();
                            System.IO.File.WriteAllBytes("/tmp/wut.png", png);
                            await ws.SendAsync(new ArraySegment<byte>(png), WebSocketMessageType.Binary, true,
                                CancellationToken.None);
                        }
                        else
                        {
                            await ws.SendJson(new {status = "noframe"});
                        }
                    }
                    else
                    {
                        await ws.SendJson(new {error = previewerSession.error});
                    }
                    await ws.ReceiveStringMessage();
                }
            }
        }
    }
}