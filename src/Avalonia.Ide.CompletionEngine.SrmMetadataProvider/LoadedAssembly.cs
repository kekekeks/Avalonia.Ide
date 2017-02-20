using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    class LoadedAssembly : IDisposable
    {
        public string AssemblyName { get; }
        private readonly MetadataBlockHolder _holder;
        public MetadataReader Reader => _holder.Reader;

        public Dictionary<string, TypeDefinition> ExportedTypes { get; } = new Dictionary<string, TypeDefinition>();

        LoadedAssembly(MetadataBlockHolder holder)
        {
            _holder = holder;
            AssemblyName = Reader.GetString(Reader.GetAssemblyDefinition().Name);
            LoadExportedTypes();
        }

        private void LoadExportedTypes()
        {
            foreach (var handle in Reader.TypeDefinitions)
            {
                var def = Reader.GetTypeDefinition(handle);
                if (def.Name.IsNil || def.Namespace.IsNil)
                    continue;
                ExportedTypes[Reader.GetString(def.Name) + "." + Reader.GetString(def.Namespace)]
                    = def;
            }
        }


        public static LoadedAssembly FromFile(string path)
        {
            using (var s = File.OpenRead(path))
            using (var pe = new System.Reflection.PortableExecutable.PEReader(s))
            {
                if (!pe.HasMetadata)
                    return null;
                var holder = new MetadataBlockHolder(pe.GetMetadata());
                if (!holder.Reader.IsAssembly)
                {
                    holder.Dispose();
                    return null;
                }
                return new LoadedAssembly(holder);
                
            }
        }

        public void Dispose()
        {
            _holder?.Dispose();
        }
    }
}