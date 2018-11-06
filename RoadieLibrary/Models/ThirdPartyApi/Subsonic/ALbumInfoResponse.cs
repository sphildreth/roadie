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
    public class AlbumInfoXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "albumInfo")]
        public albuminfo albumInfo { get; set; }
    }

    [DataContract]
    public class AlbumInfoJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public AlbumInfoResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class AlbumInfoResponse : BaseResponse
    {
        [DataMember(Name = "albumInfo")]
        public albuminfo albumInfo { get; set; }
    }

    [DataContract]
    public class albuminfo
    {
        [DataMember(Name = "notes")]
        [XmlTextAttribute()]
        public string notes { get; set; }

        [DataMember(Name = "musicBrainzId")]
        [XmlElement(ElementName = "musicBrainzId")]
        public string musicBrainzId { get; set; }

        [DataMember(Name = "lastFmUrl")]
        [XmlElement(ElementName = "lastFmUrl")]
        public string lastFmUrl { get; set; }

        [DataMember(Name = "smallImageUrl")]
        [XmlElement(ElementName = "smallImageUrl")]
        public string smallImageUrl { get; set; }

        [DataMember(Name = "mediumImageUrl")]
        [XmlElement(ElementName = "mediumImageUrl")]
        public string mediumImageUrl { get; set; }

        [DataMember(Name = "largeImageUrl")]
        [XmlElement(ElementName = "largeImageUrl")]
        public string largeImageUrl { get; set; }


    }




}
