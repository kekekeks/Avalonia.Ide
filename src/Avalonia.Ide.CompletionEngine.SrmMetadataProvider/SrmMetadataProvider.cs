using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    public class SrmMetadataProvider : IMetadataProvider
    {
        private readonly string _directory;

        public SrmMetadataProvider(string directory)
        {
            _directory = directory;
        }

        public IMetadataReaderSession GetMetadata() => new SrmMetadataProviderSession(_directory);
    }

    class SrmMetadataProviderSession : IMetadataReaderSession
    {

        private readonly Resolver _resolver;

        public SrmMetadataProviderSession(string directory)
        {
            _resolver = new Resolver();
            {
                var files = Directory.GetFiles(directory).Where(f => f.EndsWith(".dll") || f.EndsWith(".exe"));
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
