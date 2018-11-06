using System.Reflection.Metadata;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    internal class PropertyInformation : IPropertyInformation
    {
        public PropertyInformation(AssemblyInformation assembly, TypeDefinition tdef,
            PropertyDefinitionHandle handle)
        {
            var def = assembly.Reader.GetPropertyDefinition(handle);
            Name = assembly.Reader.GetString(def.Name);

            MethodInformation build(MethodDefinitionHandle mh)
                => mh.IsNil ? default(MethodInformation) : new MethodInformation(assembly, mh);

            var accessors = def.GetAccessors();

            var getter = build(accessors.Getter);
            var setter = build(accessors.Setter);

            HasPublicSetter = setter?.IsPublic ?? false;
            HasPublicGetter = getter?.IsPublic ?? false;

            TypeFullName = getter?.ReturnTypeFullName ?? setter?.Parameters[0].TypeFullName;
            IsStatic = getter?.IsStatic ?? setter?.IsStatic ?? false;
        }

        public bool IsStatic { get; }
        public bool HasPublicSetter { get; }
        public bool HasPublicGetter { get; }
        public string TypeFullName { get; }
        public string Name { get; }
    }
}