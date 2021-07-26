using System.Collections.Concurrent;
using System.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Avalonia.Ide.LanguageServer.Document
{
    /// <summary>
    /// Contains latest version of each file
    /// </summary>
    class TextDocumentBuffer
    {
        private readonly ConcurrentDictionary<DocumentUri, Buffer> _buffers = new ConcurrentDictionary<DocumentUri, Buffer>();

        public void CreateBuffer(TextDocumentIdentifier id, string data)
        {
            _buffers[id.Uri] = new Buffer(id, data);
        }

        public void UpdateBuffer(TextDocumentIdentifier id, int position, string newText, int charactersToRemove = 0)
        {
            if(!_buffers.TryGetValue(id.Uri, out Buffer buffer))
            {
                return;
            }

            if(charactersToRemove > 0)
            {
                buffer.Data.Remove(position, charactersToRemove);
            }

            buffer.Data.Insert(position, newText);
        }

        public string GetBuffer(TextDocumentIdentifier id)
        {
            return _buffers.TryGetValue(id.Uri, out var buffer) ? buffer.Data.ToString() : "";
        }
    }

    class Buffer
    {
        public Buffer(TextDocumentIdentifier id, string initialString)
        {
            Data.Append(initialString);
            Url = id.Uri;
        }

        public StringBuilder Data { get; } = new StringBuilder();
        public DocumentUri Url { get; }
    }
}
