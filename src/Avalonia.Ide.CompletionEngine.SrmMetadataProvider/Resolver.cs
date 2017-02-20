using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    class Resolver : IDisposable
    {
        private readonly Dictionary<string, LoadedAssembly> _rawAsms = new Dictionary<string, LoadedAssembly>();
        private readonly Dictionary<string, AssemblyInformation> _assemblies = new Dictionary<string, AssemblyInformation>();
        private readonly Dictionary<MetadataReader, AssemblyInformation> _readersToAsms = new Dictionary<MetadataReader, AssemblyInformation>();
        public IEnumerable<MetadataReader> Readers => _rawAsms.Values.Select(v => v.Reader);
        public TypeDescProvider TypeProvider { get; }

        public Resolver()
        {
            TypeProvider = new TypeDescProvider(this);
        }

        public void Add(LoadedAssembly asm)
        {
            _rawAsms.Add(asm.AssemblyName, asm);
            var nfo = new AssemblyInformation(asm, this);
            _assemblies[nfo.Name] = nfo;
            _readersToAsms[asm.Reader] = nfo;
        }

        public IEnumerable<AssemblyInformation> Assemblies => _assemblies.Values;

        public AssemblyInformation GetInfoFromReader(MetadataReader reader) => _readersToAsms[reader];


        public void Dispose()
        {
            foreach(var asm in _rawAsms.Values)
                asm.Dispose();
            _rawAsms.Clear();
        }

        public ITypeInformation ResolveReference(AssemblyInformation ctx, EntityHandle handle)
        {
            if (handle.Kind == HandleKind.TypeDefinition)
                return ctx.TryGetType((TypeDefinitionHandle)handle);
            if (handle.Kind == HandleKind.TypeReference)
            {
                var reference = ctx.Reader.GetTypeReference((TypeReferenceHandle) handle);
                if (reference.Name.IsNil || reference.Namespace.IsNil)
                    return null;
                if (reference.ResolutionScope.Kind == HandleKind.AssemblyReference)
                {
                    var ns = ctx.Reader.GetString(reference.Namespace);
                    var name = ctx.Reader.GetString(reference.Name);
                    var asmref =
                        ctx.Reader.GetAssemblyReference((AssemblyReferenceHandle) reference.ResolutionScope);
                    var asmname = ctx.Reader.GetString(asmref.Name);
                    AssemblyInformation asm;
                    if (!_assemblies.TryGetValue(asmname, out asm))
                        return new UnresolvedTypeInformation(ns, name);
                    return
                        (ITypeInformation)asm.TryGetType(ns + "." + name) 
                        ?? new UnresolvedTypeInformation(ns, name);
                }
                //TODO: implement other resolution scoles
            }
            //TODO: Implement type specification handling
            return null;

        }
    }
}
