using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmniSharp.Extensions.JsonRpc;

namespace Avalonia.Ide.LanguageServer
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AvaloniaXamlInfoNotification
    {
        public string XamlFile { get; set; }
        public string AssemblyPath { get; set; }
        public string PreviewerPath { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AvaloniaXamlInfoRequest
    {
        public string XamlFile { get; set; }
    }

    [Serial, Method("avalonia/getXamlInfo")]
    public interface IXamlInfoRequestHandler : IRequestHandler<AvaloniaXamlInfoRequest>
    {

    }

    class AvaloniaXamlInfoRequestHandler : IXamlInfoRequestHandler
    {
        private readonly Action<AvaloniaXamlInfoRequest> _cb;

        public AvaloniaXamlInfoRequestHandler(Action<AvaloniaXamlInfoRequest> cb)
        {
            _cb = cb;
        }
        public Task Handle(AvaloniaXamlInfoRequest request, CancellationToken token)
        {
            _cb(request);
            return Task.CompletedTask;
        }
    }
}
