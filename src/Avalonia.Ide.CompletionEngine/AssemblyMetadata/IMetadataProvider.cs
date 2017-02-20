using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Ide.CompletionEngine.AssemblyMetadata
{
    public interface IMetadataProvider
    {
        IMetadataReaderSession GetMetadata();
    }

    public interface IMetadataReaderSession : IDisposable
    {
        IEnumerable<IAssemblyInformation> Assemblies { get; }
    }
}
