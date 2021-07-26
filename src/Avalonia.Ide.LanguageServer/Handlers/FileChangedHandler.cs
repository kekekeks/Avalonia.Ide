using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.LanguageServer.AssemblyMetadata;
using Avalonia.Ide.LanguageServer.ProjectModel;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using FileSystemWatcher = OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher;

namespace Avalonia.Ide.LanguageServer.Handlers
{
    class FileChangedHandler : DidChangeWatchedFilesHandlerBase
    {
        private readonly ProjectShepard _projectShepard;
        private readonly AvaloniaMetadataShepard _metadataShepard;

        public FileChangedHandler(ProjectShepard projectShepard, AvaloniaMetadataShepard metadataShepard)
        {
            _projectShepard = projectShepard;
            _metadataShepard = metadataShepard;
        }
        
        public override Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken)
        {
            foreach(var change in request.Changes)
            {
                string localPath;
                try
                {
                    localPath = change.Uri.ToUri().LocalPath;
                }
                catch
                {
                    continue; // this is invalid path
                }

                if (localPath?.EndsWith(".csproj") == true)
                {
                    if(change.Type == FileChangeType.Created)
                    {
                        _projectShepard.ProjectAdded(localPath);
                    }
                    else if(change.Type == FileChangeType.Deleted)
                    {
                        _projectShepard.ProjectRemoved(localPath);
                    }
                }
                if(localPath?.EndsWith(".dll") == true)
                {
                    string name = Path.GetFileNameWithoutExtension(localPath);
                    var projects = _projectShepard.GetProjectsByName(name);
                    if(projects.Count > 0)
                    {
                        string directory = Path.GetDirectoryName(localPath);
                        foreach(var project in projects)
                        {
                            if (directory.StartsWith(project.BinariesDirectory))
                            {
                                _metadataShepard.InvalidateMetadata(project.FilePath);
                                project.UpdateDll(localPath);
                            }
                        }
                    }
                }
            }

            return Unit.Task;
        }

        protected override DidChangeWatchedFilesRegistrationOptions CreateRegistrationOptions(DidChangeWatchedFilesCapability capability,
            ClientCapabilities clientCapabilities)
        {
            var csProjWatcher = new FileSystemWatcher()
            {
                GlobPattern = "**/*.csproj",
                Kind = WatchKind.Create | WatchKind.Delete
            };
            var dllWatcher = new FileSystemWatcher()
            {
                GlobPattern = "**/*.dll",
                Kind = WatchKind.Create | WatchKind.Change
            };
            return new DidChangeWatchedFilesRegistrationOptions()
            {
                Watchers = new Container<FileSystemWatcher>(csProjWatcher, dllWatcher)
            };
        }
    }
}
