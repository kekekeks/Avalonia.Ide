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

            var accessors = def.GetAccessors();
            MethodInformation accessor;
            if (!accessors.Setter.IsNil)
            {
                accessor = new MethodInformation(assembly, accessors.Setter);
                HasPublicSetter = accessor.IsPublic;

                TypeFullName = accessor.Parameters[0].TypeFullName;
            }
            else
            {
                accessor = new MethodInformation(assembly, accessors.Getter);
                TypeFullName = accessor.ReturnTypeFullName;
            }
            IsStatic = accessor.IsStatic;
        }

        public bool IsStatic { get; }
        public bool HasPublicSetter { get; }
        public string TypeFullName { get; }
        public string Name { get; }
    }
}