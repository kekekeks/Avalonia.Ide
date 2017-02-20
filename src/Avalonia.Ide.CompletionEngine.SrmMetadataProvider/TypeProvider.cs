using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    class TypeDesc
    {
        public TypeDesc()
        {
            IsUnknown = true;
        }

        public TypeDesc(PrimitiveTypeCode code)
        {
            TypeCode = code;
            IsPrimitive = true;
        }

        
        private TypeDesc(ITypeInformation type)
        {
            TypeInfo = type;
        }

        public static TypeDesc FromInfo(ITypeInformation nfo)
        {
            if(nfo == null)
                return new TypeDesc();
            return new TypeDesc(nfo);
        }

        private TypeDesc(TypeDesc desc)
        {
            MetaType = desc;
            IsArray = true;
        }

        public TypeDesc(TypeDesc generic, ImmutableArray<TypeDesc> args)
        {
            MetaType = generic;
            IsGenericInstanciation = true;
            GenericParameters = args;
        }

        public static TypeDesc SystemType { get; } = new TypeDesc {IsUnknown = false, IsSystemType = true};
        public static TypeDesc MakeArray(TypeDesc of) => new TypeDesc(of);
        public bool IsUnknown { get; private set; }
        public bool IsSystemType { get; private set; }
        public bool IsGenericInstanciation { get; private set; }
        public bool IsPrimitive { get; }
        public bool IsArray { get; }
        public TypeDesc MetaType { get; }
        public ImmutableArray<TypeDesc> GenericParameters { get; }
        public PrimitiveTypeCode TypeCode { get;  }
        public ITypeInformation TypeInfo { get;  }

        public string FullName
        {
            get
            {
                if (IsPrimitive)
                    return "System." + TypeCode;
                if (IsUnknown)
                    return "<Unresolved>";
                if (TypeInfo != null)
                    return TypeInfo.FullName;
                if (IsArray)
                    return MetaType.FullName + "[]";
                if (IsGenericInstanciation)
                    return MetaType.FullName + "<" + string.Join(",", GenericParameters.Select(p => p.FullName)) + ">";
                return "<Unknown>";
            }
        }
    }

    class GenericContext
    {
        
    }

    class TypeDescProvider : ICustomAttributeTypeProvider<TypeDesc>, ISignatureTypeProvider<TypeDesc, GenericContext>
    {
        private readonly Resolver _resolver;

        public TypeDescProvider(Resolver resolver)
        {
            _resolver = resolver;
        }

        public TypeDesc GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return new TypeDesc(typeCode);
        }

        public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            => TypeDesc.FromInfo(_resolver.ResolveReference(_resolver.GetInfoFromReader(reader), handle));

        public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            => TypeDesc.FromInfo(_resolver.ResolveReference(_resolver.GetInfoFromReader(reader), handle));

        public TypeDesc GetSZArrayType(TypeDesc elementType) => TypeDesc.MakeArray(elementType);

        public TypeDesc GetSystemType() => TypeDesc.SystemType;

        public bool IsSystemType(TypeDesc type) => type.IsSystemType;

        public PrimitiveTypeCode GetUnderlyingEnumType(TypeDesc type) => PrimitiveTypeCode.Int32;

        public TypeDesc GetTypeFromSerializedName(string name) =>
            new TypeDesc();

        public TypeDesc GetGenericInstantiation(TypeDesc genericType, ImmutableArray<TypeDesc> typeArguments)
            => new TypeDesc(genericType, typeArguments);

        public TypeDesc GetArrayType(TypeDesc elementType, ArrayShape shape) => GetSZArrayType(elementType);

        public TypeDesc GetByReferenceType(TypeDesc elementType) => elementType;

        public TypeDesc GetPointerType(TypeDesc elementType) => elementType;
    

        public TypeDesc GetFunctionPointerType(MethodSignature<TypeDesc> signature)=>new TypeDesc();

        public TypeDesc GetGenericMethodParameter(GenericContext genericContext, int index)=>new TypeDesc();

        public TypeDesc GetGenericTypeParameter(GenericContext genericContext, int index) => new TypeDesc();

        public TypeDesc GetModifiedType(TypeDesc modifier, TypeDesc unmodifiedType, bool isRequired) => unmodifiedType;

        public TypeDesc GetPinnedType(TypeDesc elementType) => elementType;

        public TypeDesc GetTypeFromSpecification(MetadataReader reader, GenericContext genericContext, TypeSpecificationHandle handle,
            byte rawTypeKind)
        {
            return new TypeDesc();
        }
    }
}
