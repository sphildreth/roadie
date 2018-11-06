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
    public class IndexXMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "indexes")]
        public Indexes indexes { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class ArtistsXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "artists")]
        public artists artists { get; set; }
    }

    [DataContract]
    public class IndexJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public IndexJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class IndexJsonResponse : BaseResponse
    {
        [DataMember(Name = "indexes")]
        public Indexes indexes { get; set; }
    }

    [DataContract]
    public class ArtistsJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public ArtistsJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class ArtistsJsonResponse : BaseResponse
    {
        [DataMember(Name = "artists")]
        public artists artists { get; set; }
    }

    [DataContract]
    public class artists
    {
        [DataMember(Name = "lastModified")]
        [XmlAttribute(AttributeName = "lastModified")]
        public string lastModified { get; set; }
        [DataMember(Name = "ignoredArticles")]
        [XmlAttribute(AttributeName = "ignoredArticles")]
        public string ignoredArticles { get; set; }
        [XmlElement(ElementName = "index")]
        [DataMember(Name = "index")]
        public Index[] index { get; set; }
    }

    [DataContract]
    public class Indexes
    {
        [DataMember(Name = "lastModified")]
        [XmlAttribute(AttributeName = "lastModified")]
        public string lastModified { get; set; }
        [DataMember(Name = "ignoredArticles")]
        [XmlAttribute(AttributeName = "ignoredArticles")]
        public string ignoredArticles { get; set; }
        [XmlElement(ElementName = "index")]
        [DataMember(Name = "index")]
        public Index[] index { get; set; }
    }

    [DataContract]
    public class Index
    {
        [DataMember(Name = "name")]
        [XmlAttribute(AttributeName = "name")]
        public string name { get; set; }
        [XmlElement(ElementName = "artist")]
        [DataMember(Name = "artist")]
        public artist[] artist { get; set; }
    }

}
