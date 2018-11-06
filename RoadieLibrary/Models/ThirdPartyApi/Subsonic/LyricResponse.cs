using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class LyricXMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "lyrics")]
        public Lyric lyrics { get; set; }
    }

    public class LyricJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public LyricJsonResponse subsonicresponse { get; set; }
    }

    public class LyricJsonResponse : BaseResponse
    {
        [DataMember(Name = "lyrics")]
        public Lyric lyrics { get; set; }
    }

    public class Lyric
    {
        [DataMember(Name = "artist")]
        [XmlAttribute(AttributeName = "artist")]
        public string artist { get; set; }

        [DataMember(Name = "title")]
        [XmlAttribute(AttributeName = "title")]
        public string title { get; set; }

        [DataMember(Name = "value")]
        [XmlTextAttribute()]
        public string value { get; set; }
    }
}