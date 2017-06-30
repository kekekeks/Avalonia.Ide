using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Ide.CompletionEngine
{
    public class Completion
    {
        public string DisplayText { get; }
        public string InsertText { get; }
        public string Description { get; }
        public int? RecommendedCursorOffset { get; }

        public Completion(string displayText, string insertText, string description, int? recommendedCursorOffset = null)
        {
            DisplayText = displayText;
            InsertText = insertText;
            Description = description;
            RecommendedCursorOffset = recommendedCursorOffset;
        }

        public Completion(string insertText) : this(insertText, insertText, insertText)
        {
            
        }
    }
}
