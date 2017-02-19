using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Ide.CompletionEngine
{
    public class CompletionSet
    {
        public int StartPosition { get; set; }
        public List<Completion> Completions { get; set; }
    }
}
