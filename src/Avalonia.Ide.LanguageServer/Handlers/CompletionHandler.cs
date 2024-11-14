using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.LanguageServer.AssemblyMetadata;
using Avalonia.Ide.LanguageServer.Document;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PimpMyAvalonia.LanguageServer;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Avalonia.Ide.LanguageServer.Handlers
{
    internal class CompletionHandler : ICompletionHandler
    {
        private readonly TextDocumentBuffer _bufferManager;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter
            {
                Pattern = "**/*.axaml"
            },
            new DocumentFilter
            {
                Pattern = "**/*.xaml"
            }
        );

        private readonly ILogger<CompletionHandler> _logger;
        private readonly DocumentMetadataProvider _metadataProvider;

        public CompletionHandler(TextDocumentBuffer bufferManager, DocumentMetadataProvider metadataProvider,
            ILogger<CompletionHandler> logger)
        {
            _bufferManager = bufferManager;
            _metadataProvider = metadataProvider;
            _logger = logger;
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            string documentPath = request.TextDocument.Uri.ToUri().LocalPath;
            string buffer = _bufferManager.GetBuffer(request.TextDocument.Uri);

            if (string.IsNullOrWhiteSpace(buffer))
            {
                return new CompletionList();
            }

            var metadata = _metadataProvider.GetMetadataForDocument(documentPath);
            if (metadata == null) return new CompletionList();

            int position = Utils.PositionToOffset(request.Position, buffer.AsSpan());
            var completionResult = new CompletionEngine.CompletionEngine().GetCompletions(metadata, buffer, position);

            if (completionResult == null) return new CompletionList();

            var mappedCompletions = new List<CompletionItem>();
            for (int i = 0; i < completionResult.Completions.Count; i++)
            {
                var completion = MapCompletion(completionResult.Completions[i], completionResult.StartPosition,
                    request.Position, i.ToString().PadLeft(10, '0'), buffer);
                mappedCompletions.Add(completion);
            }

            return new CompletionList(mappedCompletions);
        }
        
        public CompletionItem MapCompletion(Completion n, int startOffset, Position pos, string sortText, string buffer)
        {
            var startPosition = Utils.OffsetToPosition(startOffset, buffer);

            string newText = n.InsertText;
            var format = InsertTextFormat.PlainText;

            if (n.RecommendedCursorOffset != null)
            {
                newText = n.InsertText.Insert(n.RecommendedCursorOffset.Value, "$0");
                format = InsertTextFormat.Snippet;
            }

            var edit = new TextEdit
            {
                NewText = newText,
                Range = new Range(startPosition, pos)
            };
            var item = new CompletionItem
            {
                Kind = MapKind(n.Kind),
                Label = n.DisplayText,
                Detail = n.DisplayText,
                Documentation = n.Description,
                TextEdit = edit,
                InsertTextFormat = format,
                SortText = sortText
            };


            return item;
        }

        public CompletionItemKind MapKind(CompletionKind kind)
        {
            switch (kind)
            {
                case CompletionKind.Class:
                    return CompletionItemKind.Class;
                case CompletionKind.Property:
                    return CompletionItemKind.Property;
                case CompletionKind.AttachedProperty:
                    return CompletionItemKind.Property;
                case CompletionKind.StaticProperty:
                    return CompletionItemKind.Property;
                case CompletionKind.Namespace:
                    return CompletionItemKind.Module;
                case CompletionKind.Enum:
                    return CompletionItemKind.Enum;
                case CompletionKind.MarkupExtension:
                    return CompletionItemKind.Function;
                case CompletionKind.Event:
                    return CompletionItemKind.Event;
                case CompletionKind.AttachedEvent:
                    return CompletionItemKind.Event;
                default:
                    return CompletionItemKind.Text;
            }
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false
            };
        }
    }
}