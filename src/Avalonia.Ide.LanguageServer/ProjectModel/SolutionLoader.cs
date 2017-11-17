using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Avalonia.Ide.LanguageServer.ProjectModel
{
    public class SolutionLoader
    {
        public static List<SolutionProject> LoadProjects(string path)
        {
            var regex = new Regex(@"^Project\(""(?<type>[^""]+)""[^""]+""(?<name>[^""]+)""[^""]+""(?<path>[^""]+)");
            var rv = new List<SolutionProject>();
            var dir = Path.GetDirectoryName(path);
            foreach (var l in File.ReadAllLines(path).Where(l => l.StartsWith("Project(")))
            {
                var m = regex.Match(l);
                if (!m.Success) continue;
                var file = Path.Combine(dir, m.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar));
                if(!File.Exists(file))
                    continue;
                rv.Add(new SolutionProject
                {
                    Name = m.Groups["name"].Value,
                    FullPath = file,
                    TypeGuid = Guid.Parse(m.Groups["type"].Value.Trim('{', '}'))
                });
            }
            return rv;
        }
    }

    public class SolutionProject
    {
        public string Name { get; set; }
        public Guid TypeGuid { get; set; }
        public string FullPath { get; set; }
    }
}