using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    public class SrmMetadataProvider : IMetadataProvider
    {
        public IMetadataReaderSession GetMetadata(IEnumerable<string> paths) => new SrmMetadataProviderSession(paths.ToArray());
    }

    class SrmMetadataProviderSession : IMetadataReaderSession
    {

        private readonly Resolver _resolver;

        public SrmMetadataProviderSession(string[] files)
        {
            _resolver = new Resolver();
            {
                foreach (var file in files)
                {
                    var asm = LoadedAssembly.FromFile(file);
                    if (asm != null)
                        _resolver.Add(asm);
                }
                Assemblies = _resolver.Assemblies;
            }

        }

        public IEnumerable<IAssemblyInformation> Assemblies { get; }

        public void Dispose()
        {
            _resolver?.Dispose();
        }
    }
}
