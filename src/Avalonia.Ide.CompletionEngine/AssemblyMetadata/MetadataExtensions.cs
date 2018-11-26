using System;
using System.Collections.Generic;

namespace Avalonia.Ide.CompletionEngine.AssemblyMetadata
{
    public static class MetadataExtensions
    {
        public static IEnumerable<IPropertyInformation> GetAllProperties(this ITypeInformation type)
        {
            
            if(type == null)
                yield break;
            
            var hs = new HashSet<string>();
            foreach (var p in type.Properties)
            {
                if (hs.Add(p.Name))
                    yield return p;
            }
            foreach (var p in GetAllProperties(type.GetBaseType()))
            {
                if (hs.Add(p.Name))
                    yield return p;
            }
        }
    }
}