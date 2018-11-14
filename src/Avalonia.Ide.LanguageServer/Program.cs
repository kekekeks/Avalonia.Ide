using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.LanguageServer;
using Avalonia.Ide.LanguageServer.Editor;
using Avalonia.Ide.LanguageServer.ProjectModel;
using Avalonia.Ide.LanguageServer.Web;
using Microsoft.Extensions.Logging;

namespace Avalonia.Ide.LanguageServer
{
    partial class Program
    {
        static void Main(string[] args)
        {
            if (args.FirstOrDefault() == "--web")
            {
                RunStandalone(args[1]);
                return;
            }
            MainAsync(args).Wait();
        }

        
        static async Task RunLsp(Stream sin, Stream sout)
        {
            var server = new OmniSharp.Extensions.LanguageServer.LanguageServer(sin, sout, new LoggerFactory());
            server.OnInitialize(args =>
            {
                Console.WriteLine(args.RootPath);
                return Task.CompletedTask;
            });
            server.AddHandler(new XamlDocumentHandler(server));
            server.AddHandler(new AvaloniaServerInfoRequestHandler(() =>
            {
                server.SendNotification("avalonia/serverInfo", new AvaloniaServerInfo
                {
                    WebBaseUri = "Lal"
                });
            }));

            await server.Initialize();
            
            await server.WasShutDown;
        }

        static void RunStandalone(string solution)
        {
            var ws = new Workspace(solution);
            ws.Reload();
            Startup.Start(new EditorSessionManager(ws, true), CancellationToken.None, 9001);
            ws.Reload();
            Thread.Sleep(-1);
        }
        
        static async Task MainAsync(string[] args)
        {
            if (args.Contains("-lsp"))
            {
                await RunLsp(Console.OpenStandardInput(), Console.OpenStandardOutput());
            }
            else
            {
                var tcpServer = new TcpListener(IPAddress.Loopback, 26001);
                tcpServer.Start();
                while (true)
                {
                    var cl = await tcpServer.AcceptTcpClientAsync();
                    //var s = new ConsoleLogStream(cl.GetStream(), Console.OpenStandardError());
                    var s = cl.GetStream();
                    RunLsp(s, s).ContinueWith(t =>
                    {
                        if(t.IsFaulted)
                            Console.WriteLine(t.Exception);
                        else
                            Console.WriteLine("LSP exited");
                    });
                }
            }
            
            
        }
    }
}