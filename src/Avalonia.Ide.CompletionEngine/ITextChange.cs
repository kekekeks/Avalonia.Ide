using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Ide.CompletionEngine
{
    public interface ITextChange
    {
        int NewPosition { get; }
        string NewText { get; }
    }
}
