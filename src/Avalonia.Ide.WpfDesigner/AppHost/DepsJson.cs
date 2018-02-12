using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Designer.AppHost
{
    [DataContract]
    class DepsJson
    {
        [DataMember(Name = "runtimeTarget")]
        public DepsJsonRuntimeTarget RuntimeTarget { get; set; }

        public static DepsJson Load(string path)
        {
            using (var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                return (DepsJson)new DataContractJsonSerializer(typeof(DepsJson)).ReadObject(s);
        }
    }

    [DataContract]
    class DepsJsonRuntimeTarget
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
