using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace AvaloniaVS.IntelliSense
{
    public class Metadata
    {
        public Dictionary<string, Dictionary<string, MetadataType>> Namespaces { get; } = new Dictionary<string, Dictionary<string, MetadataType>>();

        public void AddType(string ns, MetadataType type) => Namespaces.GetOrCreate(ns)[type.Name] = type;
    }

    public class MetadataType
    {
        public bool IsMarkupExtension { get; set; }
        public bool IsStatic { get; set; }
        public bool IsEnum { get; set; }
        public string[] EnumValues { get; set; }
        public string Name { get; set; }
        public List<MetadataProperty> Properties { get; set; } = new List<MetadataProperty>();
        public bool HasAttachedProperties { get; set; }

        public static MetadataType FromTypeInformation(ITypeInformation type)
        {
            var mt = new MetadataType
            {
                Name = type.Name,
                IsStatic = type.IsStatic,
                IsMarkupExtension = MetadataLoader.IsMarkupExtension(type),
                IsEnum = type.IsEnum
                
            };
            if (mt.IsEnum)
                mt.EnumValues = type.EnumValues.ToArray();
            return mt;
        }
    }
    
    public class MetadataProperty
    {
        public string Name { get; set; }
        public MetadataType Type { get; set; }
        public bool IsAttached { get; set; }
    }
    
    public static class MetadataLoader
    {
        internal static bool IsMarkupExtension(ITypeInformation def)
        {
            while (def != null)
            {
                if (def.Namespace == "OmniXaml" && def.Name == "MarkupExtension")
                    return true;
                def = def.GetBaseType();
            }
            return false;
        }

        
        //TODO: Custom bool type
        /*
        // add easilly the bool type and other types in the future like Brushes posiibly
        var td = new CustomEnumTypeDef(typeof(bool).FullName, new[] { "True", "False" });
          typeDefs.Add(types[typeof(bool).FullName], td);
          
         */

        public static Metadata LoadMetadata(IMetadataProvider provider)
        {
            var types = new Dictionary<string, MetadataType>();
            var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
            var metadata = new Metadata();

            types.Add(typeof(bool).FullName, new MetadataType()
            {
                Name = typeof(bool).FullName,
                IsEnum = true,
                EnumValues = new[] {"True", "False"}
            });


            foreach (var asm in provider.Assemblies)
            {
                var aliases = new Dictionary<string, string>();
                foreach (
                    var attr in
                    asm.CustomAttributes.Where(a => a.TypeFullName == "Avalonia.Metadata.XmlnsDefinitionAttribute"))
                    aliases[attr.ConstructorArguments[1].Value.ToString()] =
                        attr.ConstructorArguments[0].Value.ToString();

                foreach (var type in asm.Types.Where(x => !x.IsInterface && x.IsPublic))
                {
                    var mt = types[type.FullName] = MetadataType.FromTypeInformation(type);
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
                while (typeDef != null)
                {
                    foreach (var prop in typeDef.Properties)
                    {
                        if (prop.IsStatic || !prop.HasPublicSetter)
                            continue;

                        var p = new MetadataProperty {Name = prop.Name};

                        p.Type = types.GetValueOrDefault(prop.TypeFullName);
                        
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
            }

            return metadata;
        }
    }
}
