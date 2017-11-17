using System.Collections.Generic;
using System.IO;
using Avalonia.Ide.LanguageServer.ProjectModel;

namespace Avalonia.Ide.LanguageServer.Editor
{
    public class EditorSessionManager
    {
        private readonly Workspace _w;
        private readonly bool _loadFromDisk;

        private Dictionary<string, RefCountable<EditorSession>> _sessions
            = new Dictionary<string, RefCountable<EditorSession>>();
        
        public EditorSessionManager(Workspace w, bool loadFromDisk)
        {
            _w = w;
            _loadFromDisk = loadFromDisk;
        }
        
        public IRef<EditorSession> GetSession(string path)
        {
            lock (_sessions)
            {
                if (_sessions.TryGetValue(path, out var rc) && rc.IsAlive)
                    return rc.CreateRef();
                var xaml = _loadFromDisk ? File.ReadAllText(path) : "";
                rc = new RefCountable<EditorSession>(new EditorSession(_w, path, xaml));
                _sessions[path] = rc;
                return rc.CreateRef();
            }
        }
    }
}