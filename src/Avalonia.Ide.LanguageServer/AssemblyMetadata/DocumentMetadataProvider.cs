using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.LanguageServer.ProjectModel;
using PimpMyAvalonia.LanguageServer;

namespace Avalonia.Ide.LanguageServer.AssemblyMetadata
{
    /// <summary>
    /// Providers metadata for text documents
    /// </summary>
    public class DocumentMetadataProvider
    {
        private readonly TextDocumentToProjectMapper _documentMapper;
        private readonly AvaloniaMetadataShepard _metadataRepository;
        private readonly ProjectShepard _projectShepard;

        public DocumentMetadataProvider(
            TextDocumentToProjectMapper documentMapper, 
            AvaloniaMetadataShepard metadataRepository,
            ProjectShepard projectShepard)
        {
            _documentMapper = documentMapper;
            _metadataRepository = metadataRepository;
            _projectShepard = projectShepard;
        }

        public Metadata? GetMetadataForDocument(string documentPath)
        {
            string? projectPath = _documentMapper.GetProjectForDocument(documentPath);
            if (projectPath == null)
            {
                return null;
            }

            var metadataTask = _metadataRepository.GetMetadataForProject(projectPath);
            if (!metadataTask.IsCompletedSuccessfully)
            {
                return null;
            }
            return metadataTask.Result;
        }
    }
}
