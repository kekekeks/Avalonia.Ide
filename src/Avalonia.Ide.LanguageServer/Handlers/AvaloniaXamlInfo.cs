using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmniSharp.Extensions.JsonRpc;

namespace Avalonia.Ide.LanguageServer.Handlers
{
    [Serial, Method("avalonia/xamlInfo")]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AvaloniaXamlInfoNotification : INotification
    {
        public string? XamlFile { get; set; }
        public string? AssemblyPath { get; set; }
        public string? PreviewerPath { get; set; }
    }

    [Serial, Method("avalonia/getXamlInfoRequest")]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AvaloniaXamlInfoRequest : IRequest<AvaloniaXamlInfoNotification>, IRequest<Unit>
    {
        public string? XamlFile { get; set; }
    }
    
    class AvaloniaXamlInfoHandler : IJsonRpcRequestHandler<AvaloniaXamlInfoRequest, AvaloniaXamlInfoNotification>
    {
        public Task<AvaloniaXamlInfoNotification> Handle(AvaloniaXamlInfoRequest request, CancellationToken cancellationToken)
        {
            var xamlFile = request.XamlFile;

            // TODO: get data from language client
            string? previewerPath = Environment.GetEnvironmentVariable("AvaloniaPreviewerDevPath");
            string? assemblyPath = Environment.GetEnvironmentVariable("AvaloniaPreviewerAppPath");

            if (string.IsNullOrEmpty(previewerPath) || string.IsNullOrEmpty(assemblyPath))
            {
                throw new InvalidOperationException("Define AvaloniaPreviewerDevPath and AvaloniaPreviewerAppPath");
            }

            var result = new AvaloniaXamlInfoNotification()
            {
                XamlFile = xamlFile,
                AssemblyPath = assemblyPath,
                PreviewerPath = previewerPath
            };
            return Task.FromResult(result);
        }
    }
}
