using System;
using System.IO;
using Avalonia.Ide.LanguageServer.ProjectModel;

namespace Avalonia.Ide.LanguageServer.Editor
{
    public class EditorSession : IDisposable
    {
        private readonly Workspace _workspace;
        private readonly string _path;
        private string _xaml;
        private PreviewerSession _previewer;
        private string _previewerAssemblyPath;
        public EditorSession(Workspace workspace, string path, string xaml)
        {
            _workspace = workspace;
            _path = path;
            _xaml = xaml;
        }

        public void UpdateXaml(string xaml)
        {
            _xaml = xaml;
            if(_previewer?.IsAlive == true)
                _previewer.UpdateXaml(xaml, _previewerAssemblyPath);
        }

        public (PreviewerSession? session, string error) GetPreviewerSession()
        {
            if (_previewer != null && _previewer.IsAlive)
                return (_previewer, null);
            var project = _workspace.FindProjectWithXamlFile(_path);
            if (project == null)
                return (null, $"Can't resolve C# project for file {_path}");
            if (project.TargetPath == null)
                return (null, $"Can't resolve TargetPath for {project.Name}");
            if (!File.Exists(project.TargetPath))
                return (null, $"Can't find {project.TargetPath}, make sure to compile your project");
            var s = PreviewerSessionConnector.Start(project.TargetPath);
            if (s.session == null)
                return s;
            _previewer = s.session;
            _previewerAssemblyPath = project.TargetPath;
            s.session.UpdateXaml(_xaml, project.TargetPath);
            return s;
        }

        public void Dispose()
        {
            _previewer?.Dispose();
        }
    }
}