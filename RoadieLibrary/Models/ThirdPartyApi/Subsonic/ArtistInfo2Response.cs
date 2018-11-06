using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class ArtistInfo2XmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "artistInfo2")]
        public artistinfo2 artistInfo2 { get; set; }
    }

    [DataContract]
    public class ArtistInfo2JsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public ArtistInfo2Response subsonicresponse { get; set; }
    }

    [DataContract]
    public class ArtistInfo2Response : BaseResponse
    {
        [DataMember(Name = "artistInfo2")]
        public artistinfo2 artistInfo2 { get; set; }
    }

    [DataContract]
    public class artistinfo2
    {
        [DataMember(Name = "biography")]
        [XmlElement(ElementName = "biography")]
        public string biography { get; set; }

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