using System.Collections.Generic;

namespace Avalonia.Ide.CompletionEngine
{
    public class ManipulationResult
    {
        public IList<TextManipulation> Maniplations { get; set; } = new List<TextManipulation>();
    }
}
