using System.Collections.Generic;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine
{
    public static class MetadataConverter
    {
        internal static bool IsMarkupExtension(ITypeInformation def)
        {
            while (def != null)
            {
                if (def.Name == "MarkupExtension")
                    return true;
                def = def.GetBaseType();
            }
            return false;
        }

        public static MetadataType ConvertTypeInfomation(ITypeInformation type)
        {
            var mt = new MetadataType
            {
                Name = type.Name,
                IsStatic = type.IsStatic,
                IsMarkupExtension = MetadataConverter.IsMarkupExtension(type),
                IsEnum = type.IsEnum

            };
            if (mt.IsEnum)
                mt.EnumValues = type.EnumValues.ToArray();
            return mt;
        }

        public static Metadata ConvertMetadata(IMetadataReaderSession provider)
        {
            var types = new Dictionary<string, MetadataType>();
            var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
            var metadata = new Metadata();


            PreProcessTypes(types);

            foreach (var asm in provider.Assemblies)
            {
                var aliases = new Dictionary<string, string>();

                ProcessWellKnowAliases(asm, aliases);
                ProcessCustomAttributes(asm, aliases);

                foreach (var type in asm.Types.Where(x => !x.IsInterface && x.IsPublic))
                {
                    var mt = types[type.FullName] = ConvertTypeInfomation(type);
                    typeDefs[mt] = type;
                    metadata.AddType("clr-namespace:" + type.Namespace + ";assembly=" + asm.Name, mt);
                    string alias = null;
                    if (aliases.TryGetValue(type.Namespace, out alias))
                        metadata.AddType(alias, mt);
                }
            }

            foreach (var type in types.Values)
            {
                ITypeInformation typeDef;
                typeDefs.TryGetValue(type, out typeDef);

                var ctors = typeDef?.Methods
                    .Where(m => m.IsPublic && !m.IsStatic && m.Name == ".ctor" && m.Parameters.Count == 1);

                if (ctors?.Any() == true)
                {
                    bool supportType = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Type");
                    bool supportObject = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Object" ||
                                                        m.Parameters[0].TypeFullName == "System.String");

                    if (supportType && supportObject)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Any;
                    else if (supportType)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Type;
                    else if(supportObject)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Object;
                }

                while (typeDef != null)
                {
                    foreach (var prop in typeDef.Properties)
                    {
                        if (!prop.HasPublicGetter && !prop.HasPublicSetter)
                            continue;

                        var p = new MetadataProperty
                        {
                            Name = prop.Name,
                            Type = types.GetValueOrDefault(prop.TypeFullName),
                            IsStatic = prop.IsStatic,
                            HasGetter = prop.HasPublicGetter,
                            HasSetter = prop.HasPublicSetter
                        };

                        type.Properties.Add(p);
                    }
                    foreach (var methodDef in typeDef.Methods)
                    {
                        if (methodDef.Name.StartsWith("Set") && methodDef.IsStatic && methodDef.IsPublic
                            && methodDef.Parameters.Count == 2)
                        {
                            type.Properties.Add(new MetadataProperty()
                            {
                                Name = methodDef.Name.Substring(3),
                                IsAttached = true,
                                Type = types.GetValueOrDefault(methodDef.Parameters[1].TypeFullName)
                            });
                        }
                    }
                    typeDef = typeDef.GetBaseType();
                }

                type.HasAttachedProperties = type.Properties.Any(p => p.IsAttached);
                type.HasStaticGetProperties = type.Properties.Any(p => p.IsStatic && p.HasGetter);
            }

            PostProcessTypes(types);

            return metadata;
        }

        private static void ProcessCustomAttributes(IAssemblyInformation asm, Dictionary<string, string> aliases)
        {
            foreach (
                var attr in
                asm.CustomAttributes.Where(a => a.TypeFullName == "Avalonia.Metadata.XmlnsDefinitionAttribute" ||
                                                a.TypeFullName == "Portable.Xaml.Markup.XmlnsDefinitionAttribute"))
                aliases[attr.ConstructorArguments[1].Value.ToString()] =
                    attr.ConstructorArguments[0].Value.ToString();
        }

        private static void ProcessWellKnowAliases(IAssemblyInformation asm, Dictionary<string, string> aliases)
        {
            //some internal support for aliases
            //look like we don't have xmlns for avalonia.layout TODO: add it in avalonia
            aliases["Avalonia.Layout"] = "https://github.com/avaloniaui";
        }

        private static void PreProcessTypes(Dictionary<string, MetadataType> types)
        {
            types.Add(typeof(bool).FullName, new MetadataType()
            {
                Name = typeof(bool).FullName,
                IsEnum = true,
                EnumValues = new[] { "True", "False" }
            });
        }

        private static void PostProcessTypes(Dictionary<string, MetadataType> types)
        {
            if(types.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension", out MetadataType bindingType))
            {
                bindingType.SupportCtorArgument = MetadataTypeCtorArgument.None;
            }

            if (types.TryGetValue("Portable.Xaml.Markup.TypeExtension", out MetadataType typeExtension))
            {
                bindingType.SupportCtorArgument = MetadataTypeCtorArgument.Type;
            }
        }
    }
}
