using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class SongsByGenreXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "songsByGenre")]
        public SongsByGenreResponse songsByGenre { get; set; }
    }

    [DataContract]
    public class SongsByGenreJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public SongsByGenreJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class SongsByGenreJsonResponse : BaseResponse
    {
        [DataMember(Name = "songsByGenre")]
        [XmlElement(ElementName = "songsByGenre")]
        public SongsByGenreResponse songsByGenre { get; set; }
    }

    [DataContract]
    public class SongsByGenreResponse : BaseResponse
    {
        [DataMember(Name = "song")]
        [XmlElement(ElementName = "song")]
        public song[] song { get; set; }
    }




}
