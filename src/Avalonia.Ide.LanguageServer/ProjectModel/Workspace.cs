using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Ide.LanguageServer.MSBuild.Requests;

namespace Avalonia.Ide.LanguageServer.ProjectModel
{
    public class Workspace
    {
        private readonly string _path;

        public Workspace(string path)
        {
            _path = Path.GetFullPath(path);
        }
        
        public List<WorkspaceProject2> Projects { get; set; }

        public void Reload()
        {
            var projects = SolutionLoader.LoadProjects(_path);
            var solution = new List<WorkspaceProject2>();
            foreach (var p in projects)
            {
                var loaded = LoadProject(p);
                solution.Add(loaded);
            }
            Projects = solution;
        }

        WorkspaceProject2 LoadProject(SolutionProject p)
        {
            var resp = Globals.MsBuildHost.SendRequest(new ProjectInfoRequest
            {
                FullPath = p.FullPath,
                SolutionDirectory = Path.GetDirectoryName(_path),
                TargetFramework = "netcoreapp2.0"
            });
            var xaml = new List<string>();
            var dir = Path.GetDirectoryName(p.FullPath);
            foreach (var xf in resp.EmbeddedResources)
                if (xf.EndsWith(".xaml") || xf.EndsWith(".paml") || xf.EndsWith(".axaml"))
                {
                    var fullXamlPath = Path.Combine(dir, xf);
                    if (File.Exists(fullXamlPath))
                        xaml.Add(fullXamlPath);
                }

            return new WorkspaceProject2
            {
                Name = p.Name,
                TargetPath = resp.TargetPath,
                XamlFiles = new HashSet<string>(xaml)
            };
        }

        public WorkspaceProject2 FindProjectWithXamlFile(string path) 
            => Projects.FirstOrDefault(p => p.XamlFiles.Contains(path));
    }   
    
    public class WorkspaceProject2
    {
        public string Name { get; set; }
        public HashSet<string> XamlFiles { get; set; }
        public string TargetPath { get; set; }
    }
}