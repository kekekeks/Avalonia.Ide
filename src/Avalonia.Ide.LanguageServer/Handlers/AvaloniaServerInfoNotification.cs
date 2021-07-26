using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmniSharp.Extensions.JsonRpc;

namespace Avalonia.Ide.LanguageServer.Handlers
{

    [Serial]
    [Method("avalonia/serverInfo", Direction.ServerToClient)]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AvaloniaServerInfoNotification : INotification
    {
        public string? WebBaseUri { get; set; }
    }

    [Serial]
    [Method("avalonia/getServerInfoRequest", Direction.ClientToServer)]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AvaloniaServerInfoRequest : IRequest<AvaloniaServerInfoNotification>
    {
    }

    class AvaloniaServerInfoHandler : IJsonRpcRequestHandler<AvaloniaServerInfoRequest, AvaloniaServerInfoNotification>
    {
        public Task<AvaloniaServerInfoNotification> Handle(AvaloniaServerInfoRequest request, CancellationToken cancellationToken)
        {
            var info = new AvaloniaServerInfoNotification
            {
                WebBaseUri = "Lal"
            };

            return Task.FromResult(info);
        }
    }
}