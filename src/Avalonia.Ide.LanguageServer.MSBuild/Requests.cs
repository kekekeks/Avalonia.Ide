using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Avalonia.Ide.LanguageServer.MSBuild.Requests
{
    [DataContract]
    public class RequestBase<T>
    {
        
    }
    
    [DataContract]
    public class ResponseEnvelope<T>
    {
        [DataMember]
        public T Response { get; set; }
        [DataMember]
        public string Exception { get; set; }

        public ResponseEnvelope()
        {
            
        }

        public ResponseEnvelope(T response, string exception)
        {
            Response = response;
            Exception = exception;
        }
    }

    [DataContract]
    public class ProjectInfoRequest : RequestBase<ProjectInfoResponse>
    {
        [DataMember]
        public string FullPath { get; set; }
        [DataMember]
        public string SolutionDirectory { get; set; }
        [DataMember]
        public string TargetFramework { get; set; }
    }

    [DataContract]
    public class BuildProjectRequest : RequestBase<BuildProjectResponse>
    {
        [DataMember]
        public string FullPath { get; set; }
        [DataMember]
        public string SolutionDirectory { get; set; }
        [DataMember]
        public string TargetFramework { get; set; }
        [DataMember]
        public string Configuration { get; set; }
    }

    [DataContract]
    public class BuildProjectResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public List<string> OutputAssemblies { get; set; }
    }

    
    [DataContract]
    public class ProjectInfoResponse
    {
        [DataMember]
        public string TargetPath { get; set; }
        
        [DataMember]
        public List<string> EmbeddedResources { get; set; }
        
        [DataMember]
        public List<string> AvaloniaResources { get; set; }

        [DataMember]
        public List<string> MetaDataReferences { get; set; }

        [DataMember]
        public List<string> CscCommandLine { get; set; }
    }

    [DataContract]
    public class NextRequestType
    {
        [DataMember]
        public string TypeName { get; set; }
    }
}