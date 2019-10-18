using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine
{
    public static class MetadataConverter
    {
        internal static bool IsMarkupExtension(ITypeInformation type)
        {
            var def = type;

            while (def != null)
            {
                if (def.Name == "MarkupExtension")
                    return true;
                def = def.GetBaseType();
            }

            //in avalonia 0.9 there is no required base class, but convention only
            if (type.FullName.EndsWith("Extension") && type.Methods.Any(m => m.Name == "ProvideValue"))
            {
                return true;
            }

            return false;
        }

        public static MetadataType ConvertTypeInfomation(ITypeInformation type)
        {
            var mt = new MetadataType
            {
                Name = type.Name,
                IsStatic = type.IsStatic,
                IsMarkupExtension = IsMarkupExtension(type),
                IsEnum = type.IsEnum,
                HasHintValues = type.IsEnum

            };
            if (mt.IsEnum)
                mt.HintValues = type.EnumValues.ToArray();
            return mt;
        }

        public static Metadata ConvertMetadata(IMetadataReaderSession provider)
        {
            var types = new Dictionary<string, MetadataType>();
            var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
            var metadata = new Metadata();
            var resourceUrls = new List<string>();

            var ignoredResExt = new[] { ".resources", ".rd.xml" };

            bool skipRes(string res) => ignoredResExt.Any(r => res.EndsWith(r, StringComparison.OrdinalIgnoreCase));

            PreProcessTypes(types, metadata);

            foreach (var asm in provider.Assemblies)
            {
                var aliases = new Dictionary<string, string[]>();

                ProcessWellKnowAliases(asm, aliases);
                ProcessCustomAttributes(asm, aliases);

                foreach (var type in asm.Types.Where(x => !x.IsInterface && x.IsPublic))
                {
                    var mt = types[type.FullName] = ConvertTypeInfomation(type);
                    typeDefs[mt] = type;
                    metadata.AddType("clr-namespace:" + type.Namespace + ";assembly=" + asm.Name, mt);
                    string[] nsAliases = null;
                    if (aliases.TryGetValue(type.Namespace, out nsAliases))
                        foreach (var alias in nsAliases) metadata.AddType(alias, mt);
                }

                resourceUrls.AddRange(asm.ManifestResourceNames.Where(r => !skipRes(r)).Select(r => $"resm:{r}?assembly={asm.Name}"));
            }

            foreach (var type in types.Values)
            {
                ITypeInformation typeDef;
                typeDefs.TryGetValue(type, out typeDef);

                var ctors = typeDef?.Methods
                    .Where(m => m.IsPublic && !m.IsStatic && m.Name == ".ctor" && m.Parameters.Count == 1);

                int level = 0;
                while (typeDef != null)
                {
                    foreach (var prop in typeDef.Properties)
                    {
                        if (!prop.HasPublicGetter && !prop.HasPublicSetter)
                            continue;

                        var p = new MetadataProperty(prop.Name, types.GetValueOrDefault(prop.TypeFullName),
                            types.GetValueOrDefault(typeDef.FullName), false, prop.IsStatic, prop.HasPublicGetter,
                            prop.HasPublicSetter);

                        type.Properties.Add(p);
                    }

                    //check for attached properties only on top level
                    if (level == 0)
                    {
                        foreach (var methodDef in typeDef.Methods)
                        {
                            if (methodDef.Name.StartsWith("Set") && methodDef.IsStatic && methodDef.IsPublic
                                && methodDef.Parameters.Count == 2)
                            {
                                var name = methodDef.Name.Substring(3);
                                type.Properties.Add(new MetadataProperty(name,
                                    types.GetValueOrDefault(methodDef.Parameters[1].TypeFullName),
                                    types.GetValueOrDefault(typeDef.FullName),
                                    true, false, true, true));
                            }
                        }
                    }

                    if (typeDef.FullName == "Avalonia.AvaloniaObject")
                    {
                        type.IsAvaloniaObjectType = true;
                    }

                    typeDef = typeDef.GetBaseType();
                    level++;
                }

                type.HasAttachedProperties = type.Properties.Any(p => p.IsAttached);
                type.HasStaticGetProperties = type.Properties.Any(p => p.IsStatic && p.HasGetter);
                type.HasSetProperties = type.Properties.Any(p => !p.IsStatic && p.HasSetter);

                if (ctors?.Any() == true)
                {
                    bool supportType = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Type");
                    bool supportObject = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Object" ||
                                                        m.Parameters[0].TypeFullName == "System.String");

                    if (types.TryGetValue(ctors.First().Parameters[0].TypeFullName, out MetadataType parType)
                            && parType.HasHintValues)
                    {
                        type.SupportCtorArgument = MetadataTypeCtorArgument.HintValues;
                        type.HasHintValues = true;
                        type.HintValues = parType.HintValues;
                    }
                    else if (supportType && supportObject)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.TypeAndObject;
                    else if (supportType)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Type;
                    else if (supportObject)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Object;
                }
            }

            PostProcessTypes(types, metadata, resourceUrls);

            return metadata;
        }

        private static void ProcessCustomAttributes(IAssemblyInformation asm, Dictionary<string, string[]> aliases)
        {
            foreach (
                var attr in
                asm.CustomAttributes.Where(a => a.TypeFullName == "Avalonia.Metadata.XmlnsDefinitionAttribute" ||
                                                a.TypeFullName == "Portable.Xaml.Markup.XmlnsDefinitionAttribute"))
            {
                var ns = attr.ConstructorArguments[1].Value.ToString();
                var current = new[] { attr.ConstructorArguments[0].Value.ToString() };
                string[] allns = null;

                if (aliases.TryGetValue(ns, out allns))
                    allns = allns.Union(current).Distinct().ToArray();

                aliases[ns] = allns ?? current;
            }
        }

        private static void ProcessWellKnowAliases(IAssemblyInformation asm, Dictionary<string, string[]> aliases)
        {
            //look like we don't have xmlns for avalonia.layout TODO: add it in avalonia
            //may be don 't remove it for avalonia 0.7 or below for support completion for layout enums etc.
            aliases["Avalonia.Layout"] = new[] { "https://github.com/avaloniaui" };
        }

        private static void PreProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata)
        {
            var toAdd = new[]
            {
                new MetadataType()
                {
                    Name = typeof(bool).FullName,
                    HasHintValues = true,
                    HintValues = new[] { "True", "False" }
                },
                new MetadataType(){ Name = typeof(System.Uri).FullName },
                new MetadataType(){ Name = typeof(System.Type).FullName },
                new MetadataType(){ Name = "Avalonia.Media.IBrush" },
                new MetadataType(){ Name = "Avalonia.Media.Imaging.IBitmap" }
            };

            foreach (var t in toAdd)
                types.Add(t.Name, t);
        }

        private static void PostProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata, IEnumerable<string> resourceUrls)
        {
            bool rhasext(string resource, string ext) => resource.Contains(ext + "?assembly=");

            var resmType = new MetadataType()
            {
                Name = "resm:",
                IsStatic = true,
                HasHintValues = true,
                HintValues = resourceUrls.ToArray()
            };

            types.Add(resmType.Name, resmType);
            metadata.AddType(Utils.AvaloniaNamespace, resmType);

            var xamlResmType = new MetadataType()
            {
                Name = "resm:*.xaml",
                HasHintValues = true,
                HintValues = resourceUrls.Where(r => rhasext(r, ".xaml") || rhasext(r, ".paml")).ToArray()
            };

            types.Add(xamlResmType.Name, xamlResmType);
            metadata.AddType(Utils.AvaloniaNamespace, xamlResmType);

            MetadataType avProperty;

            if (types.TryGetValue("Avalonia.AvaloniaProperty", out avProperty))
            {
                var allProps = new Dictionary<string, MetadataProperty>();

                foreach (var type in types.Where(t => t.Value.IsAvaloniaObjectType))
                {
                    foreach (var v in type.Value.Properties.Where(p => p.HasSetter && p.HasGetter))
                    {
                        allProps[v.Name] = v;
                    }
                }

                avProperty.HasHintValues = true;
                avProperty.HintValues = allProps.Keys.ToArray();
            }

            //bindings related hints
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
                    Properties = templBinding.Properties,
                    SupportCtorArgument = MetadataTypeCtorArgument.HintValues,
                    HasHintValues = avProperty?.HasHintValues ?? false,
                    HintValues = avProperty?.HintValues
                };

                types["TemplateBindingExtension"] = tbext;
                metadata.AddType(Utils.AvaloniaNamespace, tbext);
            }

            if (types.TryGetValue("Portable.Xaml.Markup.TypeExtension", out MetadataType typeExtension))
            {
                typeExtension.SupportCtorArgument = MetadataTypeCtorArgument.Type;
            }

            //TODO: may be make it to load from assembly resources
            string[] commonResKeys = new string[] {
//common brushes
"ThemeBackgroundBrush","ThemeBorderLowBrush","ThemeBorderMidBrush","ThemeBorderHighBrush",
"ThemeControlLowBrush","ThemeControlMidBrush","ThemeControlHighBrush",
"ThemeControlHighlightLowBrush","ThemeControlHighlightMidBrush","ThemeControlHighlightHighBrush",
"ThemeForegroundBrush","ThemeForegroundLowBrush","HighlightBrush",
"ThemeAccentBrush","ThemeAccentBrush2","ThemeAccentBrush3","ThemeAccentBrush4",
"ErrorBrush","ErrorLowBrush",
//some other usefull
"ThemeBorderThickness", "ThemeDisabledOpacity",
"FontSizeSmall","FontSizeNormal","FontSizeLarge"
                };

            if (types.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension", out MetadataType dynRes))
            {
                dynRes.SupportCtorArgument = MetadataTypeCtorArgument.HintValues;
                dynRes.HasHintValues = true;
                dynRes.HintValues = commonResKeys;
            }

            if (types.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.StaticResourceExtension", out MetadataType stRes))
            {
                stRes.SupportCtorArgument = MetadataTypeCtorArgument.HintValues;
                stRes.HasHintValues = true;
                stRes.HintValues = commonResKeys;
            }

            //brushes
            if (types.TryGetValue("Avalonia.Media.IBrush", out MetadataType brushType) &&
                types.TryGetValue("Avalonia.Media.Brushes", out MetadataType brushes))
            {
                brushType.HasHintValues = true;
                brushType.HintValues = brushes.Properties.Where(p => p.IsStatic && p.HasGetter).Select(p => p.Name).ToArray();
            }

            if (types.TryGetValue("Avalonia.Styling.Selector", out MetadataType styleSelector))
            {
                styleSelector.HasHintValues = true;
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

                styleSelector.HintValues = hints.ToArray();
            }

            string[] bitmaptypes = new[] { ".jpg", ".bmp", ".png", ".ico" };

            bool isbitmaptype(string resource) => bitmaptypes.Any(ext => rhasext(resource, ext));

            if (types.TryGetValue("Avalonia.Media.Imaging.IBitmap", out MetadataType ibitmapType))
            {
                ibitmapType.HasHintValues = true;
                ibitmapType.HintValues = resourceUrls.Where(r => isbitmaptype(r)).ToArray();
            }

            if (types.TryGetValue("Avalonia.Controls.WindowIcon", out MetadataType winIcon))
            {
                winIcon.HasHintValues = true;
                winIcon.HintValues = resourceUrls.Where(r => rhasext(r, ".ico")).ToArray();
            }

            if (types.TryGetValue("Avalonia.Markup.Xaml.Styling.StyleInclude", out MetadataType styleIncludeType))
            {
                var source = styleIncludeType.Properties.FirstOrDefault(p => p.Name == "Source");

                if (source != null)
                    source.Type = xamlResmType;
            }

            if (types.TryGetValue("Avalonia.Markup.Xaml.Styling.StyleIncludeExtension", out MetadataType styleIncludeExtType))
            {
                var source = styleIncludeExtType.Properties.FirstOrDefault(p => p.Name == "Source");

                if (source != null)
                    source.Type = xamlResmType;
            }

            if (types.TryGetValue(typeof(Uri).FullName, out MetadataType uriType))
            {
                uriType.HasHintValues = true;
                uriType.HintValues = resourceUrls.ToArray();
            }
        }
    }
}