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
    public class StarredXMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "starred")]
        public starredResult starred { get; set; }
    }

    [DataContract]
    public class StarredJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public starredJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class starredJsonResponse : BaseResponse
    {
        [DataMember(Name = "starred")]
        public starredResult starred { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class Starred2XMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "starred2")]
        public starredResult starred2 { get; set; }
    }

    [DataContract]
    public class Starred2JsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public starred2JsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class starred2JsonResponse : BaseResponse
    {
        [DataMember(Name = "starred2")]
        public starredResult starred2 { get; set; }
    }

    [DataContract]
    public class starredResult
    {
        [DataMember(Name = "artist")]
        [XmlElement(ElementName = "artist")]
        public artist[] artist { get; set; }
        [DataMember(Name = "album")]
        [XmlElement(ElementName = "album")]
        public album[] album { get; set; }
        [DataMember(Name = "song")]
        [XmlElement(ElementName = "song")]
        public song[] song { get; set; }
    }




}
