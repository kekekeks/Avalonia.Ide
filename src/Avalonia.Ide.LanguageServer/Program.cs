using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.LanguageServer.AssemblyMetadata;
using Avalonia.Ide.LanguageServer.Document;
using Avalonia.Ide.LanguageServer.Editor;
using Avalonia.Ide.LanguageServer.Handlers;
using Avalonia.Ide.LanguageServer.ProjectModel;
using Avalonia.Ide.LanguageServer.Web;
using Microsoft.Extensions.DependencyInjection;
using PimpMyAvalonia.LanguageServer;

namespace Avalonia.Ide.LanguageServer
{
    public class Program
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
            var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(opts =>
            {
                opts.Input = PipeReader.Create(sin);
                opts.Output = PipeWriter.Create(sout);
                opts.AddHandler(new AvaloniaServerInfoHandler());
                opts.AddHandler(new AvaloniaXamlInfoHandler());
                opts.WithHandler<TextDocumentHandler>();
                opts.WithHandler<FileChangedHandler>();
                opts.WithHandler<CompletionHandler>();

                opts.Services.AddSingleton<CompletionHandler>();
                opts.Services.AddSingleton<FileChangedHandler>();
                opts.Services.AddSingleton<TextDocumentToProjectMapper>();
                opts.Services.AddSingleton<DocumentMetadataProvider>();
                opts.Services.AddSingleton<TextDocumentHandler>();
                opts.Services.AddSingleton<TextDocumentBuffer>();
                opts.Services.AddSingleton<AvaloniaMetadataLoader>();
                opts.Services.AddSingleton<AvaloniaMetadataShepard>();
                opts.Services.AddSingleton<ProjectShepard>();
            });

            await server.Initialize(CancellationToken.None);
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
                    _ = RunLsp(s, s).ContinueWith(t =>
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