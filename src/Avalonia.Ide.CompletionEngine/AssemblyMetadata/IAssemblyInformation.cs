using System.Collections.Generic;

namespace Avalonia.Ide.CompletionEngine.AssemblyMetadata
{
    public interface IAssemblyInformation
    {
        string Name { get; }
        IEnumerable<ITypeInformation> Types { get; }
        IEnumerable<ICustomAttributeInformation> CustomAttributes { get; }
        IEnumerable<string> ManifestResourceNames { get; }
    }

    public interface ICustomAttributeInformation
    {
        string TypeFullName { get; }
        IList<IAttributeConstructorArgumentInformation> ConstructorArguments { get; }
    }

    public interface IAttributeConstructorArgumentInformation
    {
        object Value { get; }
    }

    public interface ITypeInformation
    {
        string FullName { get; }
        string Name { get; }
        string Namespace { get; }
        ITypeInformation GetBaseType();
        IEnumerable<IMethodInformation> Methods { get; }
        IEnumerable<IPropertyInformation> Properties { get; }
        bool IsEnum { get; }
        bool IsStatic { get; }
        bool IsInterface { get; }
        bool IsPublic { get; }
        bool IsGeneric { get; }
        IEnumerable<string> EnumValues { get; }
    }

    public interface IMethodInformation
    {
        bool IsStatic { get; }
        bool IsPublic { get; }
        string Name { get; }
        IList<IParameterInformation> Parameters { get;}
    }

    public interface IParameterInformation
    {
        string TypeFullName { get; }
    }

    public interface IPropertyInformation
    {
        bool IsStatic { get; }
        bool HasPublicSetter { get; }
        bool HasPublicGetter { get; }
        string TypeFullName { get; }
        string Name { get; }
    }
}
