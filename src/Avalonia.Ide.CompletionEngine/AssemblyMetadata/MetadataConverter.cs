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

            PreProcessTypes(types, metadata);

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

                    if (typeDef.FullName == "Avalonia.AvaloniaObject")
                    {
                        type.IsAvaloniaObjectType = true;
                    }

                    typeDef = typeDef.GetBaseType();
                }

                type.HasAttachedProperties = type.Properties.Any(p => p.IsAttached);
                type.HasStaticGetProperties = type.Properties.Any(p => p.IsStatic && p.HasGetter);
            }

            PostProcessTypes(types, metadata);

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

        private static void PreProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata)
        {
            types.Add(typeof(bool).FullName, new MetadataType()
            {
                Name = typeof(bool).FullName,
                IsEnum = true,
                EnumValues = new[] { "True", "False" }
            });

            types.Add("Avalonia.Media.IBrush", new MetadataType()
            {
                Name = "Avalonia.Media.IBrush",
                IsEnum = true
            });
        }

        private static void PostProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata)
        {
            if (types.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension", out MetadataType bindingType))
            {
                bindingType.SupportCtorArgument = MetadataTypeCtorArgument.None;
            }

            if (types.TryGetValue("Avalonia.Data.TemplateBinding", out MetadataType templBinding))
            {
                var tbext = new MetadataType()
                {
                    Name = "TemplateBindingExtension",
                    IsMarkupExtension = true,
                    Properties = templBinding.Properties
                };
                types["TemplateBindingExtension"] = tbext;
                metadata.AddType("https://github.com/avaloniaui", tbext);
            }

            if (types.TryGetValue("Portable.Xaml.Markup.TypeExtension", out MetadataType typeExtension))
            {
                bindingType.SupportCtorArgument = MetadataTypeCtorArgument.Type;
            }

            if (types.TryGetValue("Avalonia.Media.IBrush", out MetadataType brushType) &&
                types.TryGetValue("Avalonia.Media.Brushes", out MetadataType brushes))
            {
                brushType.EnumValues = brushes.Properties.Where(p => p.IsStatic && p.HasGetter).Select(p => p.Name).ToArray();
            }

            if (types.TryGetValue("Avalonia.AvaloniaProperty", out MetadataType avProp))
            {
                var allProps = new Dictionary<string, MetadataProperty>();

                foreach (var type in types.Where(t => t.Value.IsAvaloniaObjectType))
                {
                    foreach (var v in type.Value.Properties.Where(p => p.HasSetter && p.HasGetter))
                    {
                        allProps[v.Name] = v;
                    }
                }

                avProp.IsEnum = true;
                avProp.EnumValues = allProps.Keys.ToArray();
            }

            if (types.TryGetValue("Avalonia.Styling.Selector", out MetadataType styleSelector))
            {
                styleSelector.IsEnum = true;
                styleSelector.IsCompositeValue = true;

                List<string> hints = new List<string>();

                //some reserved words
                hints.AddRange(new[] { "/template/", ":is()", ">", "#", "." });

                //some pseudo classes
                hints.AddRange(new[]
                {
                    ":pointerover", ":pressed", ":disabled", ":focus",
                    ":selected", ":vertical", ":horizontal",
                    ":checked", ":unchecked", ":indeterminate"
                });

                hints.AddRange(types.Where(t => t.Value.IsAvaloniaObjectType).Select(t => t.Value.Name.Replace(":", "|")));

                styleSelector.EnumValues = hints.ToArray();
            }
        }
    }
}