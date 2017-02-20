using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    class TypeInformation : ITypeInformation
    {
        private readonly TypeDefinition _def;
        public AssemblyInformation Assembly { get; }
        public TypeInformation(AssemblyInformation assembly, TypeDefinitionHandle handle)
        {
            Assembly = assembly;
            _def = assembly.Reader.GetTypeDefinition(handle);
            var r = assembly.Reader;
            Name = r.GetString(_def.Name);
            Namespace = r.GetString(_def.Namespace);
            FullName = Namespace + "." + Name;
            IsInterface = (_def.Attributes & TypeAttributes.Interface) != 0;
            IsPublic = (_def.Attributes & TypeAttributes.Public) != 0;
            IsStatic = (_def.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != 0;
            if (_def.BaseType.Kind == HandleKind.TypeReference)
            {
                var baseRef = r.GetTypeReference((TypeReferenceHandle) _def.BaseType);
                if (r.GetString(baseRef.Name) == "Enum" && r.GetString(baseRef.Namespace) == "System")
                {
                    IsEnum = true;
                    var values = new List<string>();
                    foreach (var fhandle in _def.GetFields())
                    {
                        var field = r.GetFieldDefinition(fhandle);
                        if((field.Attributes & FieldAttributes.RTSpecialName) != 0)
                            continue;
                        values.Add(r.GetString(field.Name));
                    }
                    EnumValues = values.ToArray();
                }

            }
        }


        public string FullName { get; }
        public string Name { get; }
        public string Namespace { get; }

        public ITypeInformation GetBaseType()
        {
            return Assembly.Resolver.ResolveReference(Assembly, _def.BaseType);
        }

        private IEnumerable<IMethodInformation> _methods;

        public IEnumerable<IMethodInformation> Methods
        {
            get
            {
                if (_methods == null)
                    _methods = _def.GetMethods().Select(m => new MethodInformation(Assembly, m)).ToList();
                return _methods;
            }
        }

        private IEnumerable<IPropertyInformation> _properties;

        public IEnumerable<IPropertyInformation> Properties
        {
            get
            {
                if (_properties == null)
                    _properties = _def.GetProperties().Select(p => new PropertyInformation(Assembly, _def, p));
                return _properties;
            }
        }
        public bool IsEnum { get; }
        public bool IsStatic { get; }
        public bool IsInterface { get; }
        public bool IsPublic { get; }
        public IEnumerable<string> EnumValues { get; }
    }

    class UnresolvedTypeInformation : ITypeInformation
    {
        public UnresolvedTypeInformation(string ns, string name)
        {
            Namespace = ns;
            Name = name;
            FullName = Namespace + "." + Name;
        }
        public string FullName { get; }
        public string Name { get; }
        public string Namespace { get; }
        public ITypeInformation GetBaseType() => null;

        public IEnumerable<IMethodInformation> Methods  => new IMethodInformation[0];
        public IEnumerable<IPropertyInformation> Properties => new IPropertyInformation[0];
        public bool IsEnum { get; }
        public bool IsStatic { get; }
        public bool IsInterface { get; }
        public bool IsPublic { get; }
        public IEnumerable<string> EnumValues => new string[0];
    }
}