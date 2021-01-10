using Avalonia.Ide.CompletionEngine;

namespace Avalonia.Ide.LanguageServer.Document
{
    public class TextChangeAdapter : ITextChange
    {
        public TextChangeAdapter(int position, string newText, string oldText)
        {
            NewPosition = OldPosition = position;
            NewText = newText;
            OldText = oldText;
        }

        public int NewPosition { get; }

        public string NewText { get; }

        public int OldPosition { get; }

        public string OldText { get; }
    }
}
