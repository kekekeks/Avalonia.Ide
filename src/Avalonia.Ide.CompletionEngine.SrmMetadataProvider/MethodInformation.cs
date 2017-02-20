using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    class MethodInformation : IMethodInformation
    {
        private readonly AssemblyInformation _asm;
        private readonly MethodDefinition _def;

        public MethodInformation(AssemblyInformation asm, MethodDefinitionHandle handle)
        {
            _asm = asm;
            _def = asm.Reader.GetMethodDefinition(handle);
            IsStatic = (_def.Attributes & MethodAttributes.Static) != 0;
            IsPublic = (_def.Attributes & MethodAttributes.Public) == MethodAttributes.Public;
            Name = asm.Reader.GetString(_def.Name);
            
            
        }

        public bool IsStatic { get; }
        public bool IsPublic { get; }
        public string Name { get; }
        private IList<IParameterInformation> _parameters;
        private string _returnTypeFullName;

        void LoadSig()
        {
            _parameters = new List<IParameterInformation>();

            var sig = _def.DecodeSignature(_asm.Resolver.TypeProvider, null);
            foreach (var td in sig.ParameterTypes)
                _parameters.Add(new ParameterInformation(td.FullName));
            _returnTypeFullName = sig.ReturnType?.FullName;
        }
        public string ReturnTypeFullName
        {
            get
            {
                if (_returnTypeFullName == null)
                    LoadSig();
                return _returnTypeFullName;
            }
        }

        public IList<IParameterInformation> Parameters
        {
            get
            {
                if (_parameters == null)
                    LoadSig();
                return _parameters;
            }
        }

        class ParameterInformation : IParameterInformation
        {
            public ParameterInformation(string typeFullName)
            {
                TypeFullName = typeFullName;
            }
            public string TypeFullName { get; }
        }
    }
}