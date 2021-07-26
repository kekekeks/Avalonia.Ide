using System.IO;
using System.Linq;

namespace Avalonia.Ide.LanguageServer.ProjectModel
{

    public class WorkspaceProject
    {
        public string ProjectDirectory { get; }
        public string BinariesDirectory { get; }

        public string Name { get; }
        public string FilePath { get; }

        public string? OutputFile { get; private set; }

        public WorkspaceProject(string path)
        {
            FilePath = path;
            ProjectDirectory = Path.GetDirectoryName(path);
            BinariesDirectory = Path.Combine(ProjectDirectory, "bin");
            Name = Path.GetFileNameWithoutExtension(path);

            OutputFile = Directory.GetFiles(BinariesDirectory, Name + ".dll", SearchOption.AllDirectories).FirstOrDefault();


        }

        internal void UpdateDll(string path)
        {
            OutputFile = path;
        }
    }
}
