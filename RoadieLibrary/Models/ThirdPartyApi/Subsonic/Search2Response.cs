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
    public class Search2XMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "searchResult2")]
        public searchResult2 searchResult2 { get; set; }
    }


    [DataContract]
    public class Search2JsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public searchResult2JsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class searchResult2JsonResponse : BaseResponse
    {
        [DataMember(Name = "searchResult2")]
        public searchResult2 searchResult2 { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class Search3XMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "searchResult3")]
        public searchResult2 searchResult3 { get; set; }
    }


    [DataContract]
    public class Search3JsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public searchResult3JsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class searchResult3JsonResponse : BaseResponse
    {
        [DataMember(Name = "searchResult3")]
        public searchResult2 searchResult3 { get; set; }
    }

    [DataContract]
    public class searchResult2
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
