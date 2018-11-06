using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class MusicFoldersXmlResponse : BaseResponse
    {
        [DataMember(Name = "musicFolders")]
        public musicFolder[] musicFolders { get; set; }
    }

    [DataContract]
    public class MusicFoldersJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public MusicFoldersJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class MusicFoldersJsonResponse : BaseResponse
    {
        [DataMember(Name = "musicFolders")]
        public Musicfolders musicFolders { get; set; }
    }

    [DataContract]
    public class Musicfolders
    {
        [DataMember(Name = "musicFolder")]
        public musicFolder[] musicFolder { get; set; }
    }

    [DataContract]
    public class musicFolder
    {
        [DataMember(Name = "id")]
        [XmlAttribute]
        public int id { get; set; }
        [DataMember(Name = "name")]
        [XmlAttribute]
        public string name { get; set; }
    }  


}
