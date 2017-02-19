using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Ide.CompletionEngine.AssemblyMetadata
{
    public interface IMetadataProvider
    {
        IEnumerable<IAssemblyInformation> Assemblies { get; }
    }
}
