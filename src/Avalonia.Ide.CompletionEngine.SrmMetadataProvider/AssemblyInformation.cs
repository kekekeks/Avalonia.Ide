using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using MetadataReader = System.Reflection.Metadata.MetadataReader;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{

    class AssemblyInformation : IAssemblyInformation
    {
        private readonly LoadedAssembly _asm;
        public MetadataReader Reader => _asm.Reader;
        private Dictionary<string, TypeInformation> _types;
        private Dictionary<TypeDefinitionHandle, TypeInformation> _htypes;
        public Resolver Resolver { get; }
        public AssemblyInformation(LoadedAssembly asm, Resolver resolver)
        {
            _asm = asm;
            Resolver = resolver;
            Name = Reader.GetString(Reader.GetAssemblyDefinition().Name);
        }
        

        public string Name { get; }

        void LoadTypes()
        {
            if(_types != null)
                return;
            _types = new Dictionary<string, TypeInformation>();
            _htypes = new Dictionary<TypeDefinitionHandle, TypeInformation>();
            foreach (var handle in Reader.TypeDefinitions)
            {
                var nfo = new TypeInformation(this, handle);
                _htypes[handle] = _types[nfo.FullName] = nfo;
            }
            
        }

        public TypeInformation TryGetType(string fullName)
        {
            LoadTypes();
            TypeInformation rv;
            _types.TryGetValue(fullName, out rv);
            return rv;
        }

        public TypeInformation TryGetType(TypeDefinitionHandle def)
        {
            LoadTypes();
            TypeInformation rv;
            _htypes.TryGetValue(def, out rv);
            return rv;
        }

        IEnumerable<ITypeInformation> IAssemblyInformation.Types
        {
            get
            {
                LoadTypes();
                return _types.Values;
            }
        }

        IEnumerable<string> IAssemblyInformation.ManifestResourceNames
        {
            get
            {
                return Reader.ManifestResources.Where(r => !r.IsNil)
                            .Select(r => Reader.GetString(Reader.GetManifestResource(r).Name));
            }
        }

        public IEnumerable<ICustomAttributeInformation> CustomAttributes
            => Reader.CustomAttributes.Select(a => new CustomAttributeInformation(this, a));

        public override string ToString() => Name;
    }
}
