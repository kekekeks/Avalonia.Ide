using Avalonia.Ide.LanguageServer.MSBuild.Requests;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using MTask = Microsoft.Build.Utilities.Task;

namespace Avalonia.Ide.LanguageServer.MSBuild
{
    public class AvaloniaIdeTask : MTask
    {
        [Required]
        public string Port { get; set; }

        object CreateResponseEnvelope(Type t, object response, string exception)
        {
            while (!t.IsConstructedGenericType || t.GetGenericTypeDefinition() != typeof(RequestBase<>))
                t = t.GetTypeInfo().BaseType;
            t = t.GetGenericArguments()[0];
            var envelopeType = typeof(ResponseEnvelope<>).MakeGenericType(t);
            return Activator.CreateInstance(envelopeType, response, exception);
        }
        
        public override bool Execute()
        {
            var cl = new TcpClient();
            cl.ConnectAsync(IPAddress.Loopback, int.Parse(Port)).Wait();
            /*while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }*/
            var c = new WireHelper(cl.GetStream());
            while (true)
            {
                var tname = c.Read<NextRequestType>();
                var t = Type.GetType(tname.TypeName);
                var req = c.Read(t);
                object response = null;
                try
                {
                    if (req is ProjectInfoRequest pnfo)
                        response = Handle(pnfo);
                    else
                        throw new InvalidOperationException();
                }
                catch (Exception e)
                {
                    c.Send(CreateResponseEnvelope(t, null, e.ToString()));
                }
                if (response != null)
                    c.Send(CreateResponseEnvelope(t, response, null));

                Console.WriteLine("*** Request Handled");

            }
        }

        ProjectInfoResponse Handle(ProjectInfoRequest req)
        {
            var targetsPath = typeof(AvaloniaIdeTask).GetTypeInfo().Assembly.GetModules()[0].FullyQualifiedName;
            targetsPath = Path.Combine(Path.GetDirectoryName(targetsPath), "avalonia-ide.targets");
            var props = new Dictionary<string, string>
            {
                ["DesignTimeBuild"] = "true",
                ["BuildProjectReferences"] = "false",
                ["_ResolveReferenceDependencies"] = "true",
                ["SolutionDir"] = req.SolutionDirectory,
                ["ProvideCommandLineInvocation"] = "true",
                ["SkipCompilerExecution"] = "true",
                ["TargetFramework"] = req.TargetFramework,
                ["CustomBeforeMicrosoftCommonTargets"] = targetsPath
            };
            var outputs = new Dictionary<string, ITaskItem[]>();
            if (!BuildEngine.BuildProjectFile(req.FullPath, new[] { "ResolveAssemblyReferences", "GetTargetPath", "AvaloniaGetEmbeddedResources" },
                props, outputs))
                throw new Exception("Build failed");
            
            var result = new ProjectInfoResponse
            {
                TargetPath = outputs["GetTargetPath"][0].ItemSpec,
                EmbeddedResources = outputs["AvaloniaGetEmbeddedResources"].Select(x=>x.ItemSpec).ToList()
            };

            if(outputs.ContainsKey("ResolveAssemblyReferences"))
            {
                result.MetaDataReferences = outputs["ResolveAssemblyReferences"].Select(x => x.ItemSpec).ToList();
            }

            return result;
        }
    }
}