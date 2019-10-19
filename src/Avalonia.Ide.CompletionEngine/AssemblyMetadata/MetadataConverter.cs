using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using System;
using System.Collections.Generic;
using System.Linq;

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
                FullName = type.FullName,
                IsStatic = type.IsStatic,
                IsMarkupExtension = IsMarkupExtension(type),
                IsEnum = type.IsEnum,
                HasHintValues = type.IsEnum,
                IsGeneric = type.IsGeneric,
            };
            if (mt.IsEnum)
                mt.HintValues = type.EnumValues.ToArray();
            return mt;
        }

        private class AvaresInfo
        {
            public IAssemblyInformation Assembly;
            public string ReturnTypeFullName;
            public string LocalUrl;
            public string GlobalUrl;
            public override string ToString() => GlobalUrl;
        }

        public static Metadata ConvertMetadata(IMetadataReaderSession provider)
        {
            var types = new Dictionary<string, MetadataType>();
            var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
            var metadata = new Metadata();
            var resourceUrls = new List<string>();
            var avaresValues = new List<AvaresInfo>();

            var ignoredResExt = new[] { ".resources", ".rd.xml" };

            bool skipRes(string res) => ignoredResExt.Any(r => res.EndsWith(r, StringComparison.OrdinalIgnoreCase));

            PreProcessTypes(types, metadata);

            foreach (var asm in provider.Assemblies)
            {
                var aliases = new Dictionary<string, string[]>();

                ProcessWellKnownAliases(asm, aliases);
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

                const string avaresToken = "Build:"; //or "Populate:" should work both ways

                foreach (var resType in asm.Types.Where(t => t.FullName == "CompiledAvaloniaXaml.!AvaloniaResources" || t.Name == "CompiledAvaloniaXaml.!AvaloniaResources"))
                {
                    foreach (var res in resType.Methods.Where(m => m.Name.StartsWith(avaresToken)))
                    {
                        var localUrl = res.Name.Replace(avaresToken, "");

                        var avres = new AvaresInfo
                        {
                            Assembly = asm,
                            LocalUrl = localUrl,
                            GlobalUrl = $"avares://{asm.Name}{localUrl}",
                            ReturnTypeFullName = res.ReturnTypeFullName ?? ""
                        };

                        avaresValues.Add(avres);
                    }
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

            PostProcessTypes(types, metadata, resourceUrls, avaresValues);

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

        private static void ProcessWellKnownAliases(IAssemblyInformation asm, Dictionary<string, string[]> aliases)
        {
            //look like we don't have xmlns for avalonia.layout TODO: add it in avalonia
            //may be don 't remove it for avalonia 0.7 or below for support completion for layout enums etc.
            aliases["Avalonia.Layout"] = new[] { "https://github.com/avaloniaui" };
        }

        private static void PreProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata)
        {
            var toAdd = new List<MetadataType>
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
                new MetadataType(){ Name = "Avalonia.Media.Imaging.IBitmap" },
            };

            foreach (var t in toAdd)
                types.Add(t.Name, t);

            var portableXamlExtTypes = new[]
            {
                new MetadataType()
                {
                    Name = "StaticExtension",
                    SupportCtorArgument = MetadataTypeCtorArgument.Object,
                    HasSetProperties = true,
                    IsMarkupExtension = true,
                },
                new MetadataType()
                {
                    Name = "TypeExtension",
                    SupportCtorArgument = MetadataTypeCtorArgument.TypeAndObject,
                    HasSetProperties = true,
                    IsMarkupExtension = true,
                },
                new MetadataType()
                {
                    Name = "NullExtension",
                    HasSetProperties = true,
                    IsMarkupExtension = true,
                },
                new MetadataType()
                {
                    Name = "Class",
                    HasAttachedProperties = true
                },
                new MetadataType()
                {
                    Name = "Name",
                    HasAttachedProperties = true
                },
                new MetadataType()
                {
                    Name = "Key",
                    HasAttachedProperties = true
                },
            };

            //as in avalonia 0.9 Portablexaml is missing we need to hardcode some extensions
            foreach (var t in portableXamlExtTypes)
            {
                metadata.AddType(Utils.Xaml2006Namespace, t);
            }

            metadata.AddType(Utils.AvaloniaNamespace, new MetadataType() { Name = "xmlns", HasAttachedProperties = true });
        }

        private static void PostProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata, IEnumerable<string> resourceUrls, List<AvaresInfo> avaResValues)
        {
            bool rhasext(string resource, string ext) => resource.StartsWith("resm:") ? resource.Contains(ext + "?assembly=") : resource.EndsWith(ext);

            var allresourceUrls = avaResValues.Select(v => v.GlobalUrl).Concat(resourceUrls).ToArray();

            var resType = new MetadataType()
            {
                Name = "avares://,resm:",
                IsStatic = true,
                HasHintValues = true,
                HintValues = allresourceUrls
            };

            types.Add(resType.Name, resType);

            var xamlResType = new MetadataType()
            {
                Name = "avares://*.xaml,resm:*.xaml",
                HasHintValues = true,
                HintValues = resType.HintValues.Where(r => rhasext(r, ".xaml") || rhasext(r, ".paml")).ToArray()
            };

            var styleResType = new MetadataType()
            {
                Name = "Style avares://*.xaml,resm:*.xaml",
                HasHintValues = true,
                HintValues = avaResValues.Where(v => v.ReturnTypeFullName.StartsWith("Avalonia.Styling.Style")).Select(v => v.GlobalUrl)
                                        .Concat(resourceUrls.Where(r => rhasext(r, ".xaml") || rhasext(r, ".paml")))
                                        .ToArray()
            };

            types.Add(styleResType.Name, styleResType);

            IEnumerable<string> filterLocalRes(MetadataType type, string currentAssemblyName)
            {
                var localResPrefix = $"avares://{currentAssemblyName}";
                var resmSuffix = $"?assembly={currentAssemblyName}";

                foreach (var hint in type.HintValues ?? Array.Empty<string>())
                {
                    if (hint.StartsWith("avares://"))
                    {
                        if (hint.StartsWith(localResPrefix))
                        {
                            yield return hint.Substring(localResPrefix.Length);
                        }
                    }
                    else if (hint.StartsWith("resm:"))
                    {
                        if (hint.EndsWith(resmSuffix))
                        {
                            yield return hint.Substring(0, hint.Length - resmSuffix.Length);
                        }
                    }
                }
            }

            resType.CurrentAssemblyHintValuesFunc = a => filterLocalRes(xamlResType, a);
            xamlResType.CurrentAssemblyHintValuesFunc = a => filterLocalRes(xamlResType, a);
            styleResType.CurrentAssemblyHintValuesFunc = a => filterLocalRes(styleResType, a);

            types.Add(xamlResType.Name, xamlResType);

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
                ibitmapType.HintValues = allresourceUrls.Where(r => isbitmaptype(r)).ToArray();
                ibitmapType.CurrentAssemblyHintValuesFunc = a => filterLocalRes(ibitmapType, a);
            }

            if (types.TryGetValue("Avalonia.Controls.WindowIcon", out MetadataType winIcon))
            {
                winIcon.HasHintValues = true;
                winIcon.HintValues = allresourceUrls.Where(r => rhasext(r, ".ico")).ToArray();
                winIcon.CurrentAssemblyHintValuesFunc = a => filterLocalRes(winIcon, a);
            }

            if (types.TryGetValue("Avalonia.Markup.Xaml.Styling.StyleInclude", out MetadataType styleIncludeType))
            {
                var source = styleIncludeType.Properties.FirstOrDefault(p => p.Name == "Source");

                if (source != null)
                    source.Type = styleResType;
            }

            if (types.TryGetValue("Avalonia.Markup.Xaml.Styling.StyleIncludeExtension", out MetadataType styleIncludeExtType))
            {
                var source = styleIncludeExtType.Properties.FirstOrDefault(p => p.Name == "Source");

                if (source != null)
                    source.Type = xamlResType;
            }

            if (types.TryGetValue(typeof(Uri).FullName, out MetadataType uriType))
            {
                uriType.HasHintValues = true;
                uriType.HintValues = allresourceUrls.ToArray();
                uriType.CurrentAssemblyHintValuesFunc = a => filterLocalRes(uriType, a);
            }

            if (types.TryGetValue("System.Type", out MetadataType typeType))
            {
                var prop = new MetadataProperty("x:TypeArguments", typeType, null, false, false, false, true);
                foreach (var t in types.Where(t => t.Value.IsGeneric))
                    t.Value.Properties.Add(prop);
            }
        }
    }
}