using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    class CustomAttributeInformation : ICustomAttributeInformation
    {
        private readonly AssemblyInformation _asm;

        public CustomAttributeInformation(AssemblyInformation asm, CustomAttributeHandle handle)
        {
            _asm = asm;
            _ca = asm.Reader.GetCustomAttribute(handle);
            if (_ca.Constructor.Kind == HandleKind.MethodDefinition)
            {
                var mdef = asm.Reader.GetMethodDefinition((MethodDefinitionHandle) _ca.Constructor);
                var tdef = asm.Reader.GetTypeDefinition(mdef.GetDeclaringType());
                TypeFullName = asm.Reader.GetString(tdef.Namespace) + "." + asm.Reader.GetString(tdef.Name);
            }
            else
            {
                var mref = asm.Reader.GetMemberReference((MemberReferenceHandle) _ca.Constructor);
                if (mref.Parent.Kind == HandleKind.TypeReference)
                {
                    var tref = asm.Reader.GetTypeReference((TypeReferenceHandle) mref.Parent);
                    TypeFullName = asm.Reader.GetString(tref.Namespace) + "." + asm.Reader.GetString(tref.Name);
                }
            }
            
        }

        public string TypeFullName { get; }

        private IList<IAttributeConstructorArgumentInformation> _constructorArguments;
        private CustomAttribute _ca;

        public IList<IAttributeConstructorArgumentInformation> ConstructorArguments
        {
            get
            {
                if (_constructorArguments == null)
                {
                    var res = _ca.DecodeValue(_asm.Resolver.TypeProvider);
                    _constructorArguments = res.FixedArguments
                        .Select(fa => (IAttributeConstructorArgumentInformation) new ParameterInfo() {Value = fa.Value})
                        .ToList();
                }
                return _constructorArguments;
            }
        }

        class ParameterInfo : IAttributeConstructorArgumentInformation
        {
            public object Value { get; set; }
        }
    }
}