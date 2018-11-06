using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class GenresXMLResponse : BaseResponse
    {
        [DataMember(Name = "genres")]
        public genre[] genres { get; set; }
    }

    [DataContract]
    public class GenresJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public GenresJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class GenresJsonResponse : BaseResponse
    {
        [DataMember(Name = "genres")]
        public Genres genres { get; set; }
    }


    [DataContract]
    public class Genres
    {
        [DataMember(Name = "genre")]
        [XmlElement(ElementName = "genre")]
        public genre[] genre { get; set; }
    }
    [DataContract]
    public class genre
    {
        [DataMember(Name = "songCount")]
        [XmlAttribute(AttributeName = "songCount")]
        public int songCount { get; set; }
        [DataMember(Name = "albumCount")]
        [XmlAttribute(AttributeName = "albumCount")]
        public int albumCount { get; set; }
        [DataMember(Name = "value")]
        [XmlText]
        public string value { get; set; }
    }
}
