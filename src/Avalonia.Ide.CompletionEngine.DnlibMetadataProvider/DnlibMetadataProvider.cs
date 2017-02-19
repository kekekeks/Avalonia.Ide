using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using dnlib.DotNet;

namespace Avalonia.Ide.CompletionEngine.DnlibMetadataProvider
{
    public class DnlibMetadataProvider : IMetadataProvider
    {
        private Lazy<IEnumerable<IAssemblyInformation>> _assemblies;
        public IEnumerable<IAssemblyInformation> Assemblies => _assemblies.Value;
        public DnlibMetadataProvider(string directoryPath)
        {
            _assemblies = new Lazy<IEnumerable<IAssemblyInformation>>(
                () => LoadAssemblies(directoryPath).Select(a => new AssemblyWrapper(a)).ToList());
        }

        static List<AssemblyDef> LoadAssemblies(string directory)
        {
            AssemblyResolver asmResolver = new AssemblyResolver();
            ModuleContext modCtx = new ModuleContext(asmResolver);
            asmResolver.DefaultModuleContext = modCtx;
            asmResolver.EnableTypeDefCache = true;
            
            asmResolver.PreSearchPaths.Add(directory);

            List<AssemblyDef> assemblies = new List<AssemblyDef>();

            foreach (var asm in Directory.GetFiles(directory, "*.*")
                .Where(f => Path.GetExtension(f.ToLower()) == ".exe" || Path.GetExtension(f.ToLower()) == ".dll"))
            {
                try
                {
                    var def = AssemblyDef.Load(asm);
                    def.Modules[0].Context = modCtx;
                    asmResolver.AddToCache(def);
                    assemblies.Add(def);
                }
                catch
                {
                    //Ignore
                }
            }

            return assemblies;
        }

    }
}
