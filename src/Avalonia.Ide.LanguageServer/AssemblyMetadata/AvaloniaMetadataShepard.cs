using System.Collections.Concurrent;
using System.Threading.Tasks;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.LanguageServer.ProjectModel;
using PimpMyAvalonia.LanguageServer;

namespace Avalonia.Ide.LanguageServer.AssemblyMetadata
{
    /// <summary>
    /// Owns metadata loaded for given project
    /// </summary>
    public class AvaloniaMetadataShepard
    {
        private readonly AvaloniaMetadataLoader _metadataLoader;
        private readonly ProjectShepard _projectShepard;

        public AvaloniaMetadataShepard(AvaloniaMetadataLoader metadataLoader, ProjectShepard projectShepard)
        {
            _metadataLoader = metadataLoader;
            _projectShepard = projectShepard;
        }

        public ConcurrentDictionary<string, Task<Metadata?>> ProjectMetadata { get; } = new ConcurrentDictionary<string, Task<Metadata?>>();

        public Task<Metadata?> GetMetadataForProject(string projectPath)
        {
            var metadataTask = ProjectMetadata.GetOrAdd(projectPath, n => _metadataLoader.CreateMetadataForProject(_projectShepard.GetProject(n)));
            return metadataTask;
        }

        internal void InvalidateMetadata(string path)
        {
            if(ProjectMetadata.TryRemove(path, out _))
            {
                // If metadata exists regenerate it
                GetMetadataForProject(path);
            }
        }
    }
}
