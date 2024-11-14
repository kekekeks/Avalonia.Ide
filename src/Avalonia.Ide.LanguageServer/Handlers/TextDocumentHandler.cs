using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.LanguageServer.Document;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using PimpMyAvalonia.LanguageServer;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Avalonia.Ide.LanguageServer.Handlers
{
    internal class TextDocumentHandler : TextDocumentSyncHandlerBase
    {
        private readonly TextDocumentBuffer _bufferManager;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter
            {
                Pattern = "**/*.xaml"
            },
            new DocumentFilter
            {
                Pattern = "**/*.axaml"
            }
        );

        public TextDocumentHandler(TextDocumentBuffer bufferManager)
        {
            _bufferManager = bufferManager;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Incremental;

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                SyncKind = Change
            };
        }

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri documentUri)
        {
            return new TextDocumentAttributes(documentUri.ToUri(), "xml");
        }

        public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            _bufferManager.CreateBuffer(request.TextDocument, request.TextDocument.Text);
            return Unit.Task;
        }

        public override async Task<Unit> Handle(DidChangeTextDocumentParams request,
            CancellationToken cancellationToken)
        {
            string text = request.ContentChanges.FirstOrDefault()?.Text ?? "";
            string buffer = _bufferManager.GetBuffer(request.TextDocument);
            int offset = 0;
            int characterToRemove = 0;
            foreach (var change in request.ContentChanges)
            {
                if (change.Range == null)
                {
                    _bufferManager.CreateBuffer(request.TextDocument, change.Text);
                }
                else
                {
                    offset = Utils.PositionToOffset(change.Range.Start, buffer);
                    characterToRemove = 0;
                    if (change.Range.Start != change.Range.End)
                        characterToRemove = Utils.PositionToOffset(change.Range.End, buffer) - offset;

                    _bufferManager.UpdateBuffer(request.TextDocument, offset, text, characterToRemove);
                }
                
            }

            if (request.ContentChanges.Count() == 1)
            {
                string bufferWithContentChange = _bufferManager.GetBuffer(request.TextDocument);
                await ApplyTextManipulationsAsync(request, text, buffer, bufferWithContentChange, offset,
                    characterToRemove);
            }

            return Unit.Value;
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        private async Task ApplyTextManipulationsAsync(DidChangeTextDocumentParams request, string text, string buffer,
            string changedBuffer, int position, int deletedCharacters)
        {
            var textManipulator = new TextManipulator(changedBuffer, position);
            var manipulations =
                textManipulator.ManipulateText(new TextChangeAdapter(position, text,
                    buffer.Substring(position, deletedCharacters)));

            if (manipulations.Count == 0) return;

            var edits = manipulations.Select(n =>
            {
                var start = Utils.OffsetToPosition(n.Start, changedBuffer);

                switch (n.Type)
                {
                    case ManipulationType.Insert:
                        return new TextEdit
                        {
                            NewText = n.Text,
                            Range = new Range(start, start)
                        };
                    case ManipulationType.Delete:
                        var end = Utils.OffsetToPosition(n.End, changedBuffer);
                        return new TextEdit
                        {
                            NewText = "",
                            Range = new Range(start, end)
                        };
                    default:
                        throw new NotSupportedException();
                }
            }).ToList();
            // if (edits.Count > 0)
            //     await _router.ApplyWorkspaceEdit(new ApplyWorkspaceEditParams
            //     {
            //         Edit = new WorkspaceEdit
            //         {
            //             DocumentChanges = new Container<WorkspaceEditDocumentChange>(new WorkspaceEditDocumentChange(
            //                 new TextDocumentEdit
            //                 {
            //                     TextDocument = request.TextDocument,
            //                     Edits = new TextEditContainer(edits)
            //                 }))
            //         }
            //     });
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
            SynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSyncRegistrationOptions(TextDocumentSyncKind.Incremental)
            {
                Save = new BooleanOr<SaveOptions>(new SaveOptions
                {
                    IncludeText = true
                }),
                DocumentSelector = _documentSelector
            };
        }
    }
}