using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmniSharp.Extensions.JsonRpc;

namespace Avalonia.Ide.LanguageServer
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AvaloniaServerInfo
    {
        public string WebBaseUri { get; set; }
    }
    
    [Serial, Method("avalonia/getServerInfo")]
    public interface IServerInfoRequestHandler : IRequestHandler<object>
    {
        
    }

    class AvaloniaServerInfoRequestHandler : IServerInfoRequestHandler
    {
        private readonly Action _cb;

        public AvaloniaServerInfoRequestHandler(Action cb)
        {
            _cb = cb;
        }
        public Task Handle(object request, CancellationToken token)
        {
            _cb();
            return Task.CompletedTask;
        }
    }
}