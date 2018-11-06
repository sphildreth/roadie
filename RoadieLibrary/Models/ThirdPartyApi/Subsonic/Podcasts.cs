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
    public class PodcastsXmlResponse : BaseResponse
    {
        [DataMember(Name = "podcasts")]
        public Podcasts[] podcasts { get; set; }
    }

    [DataContract]
    public class PodcastsJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public PodcastsJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class PodcastsJsonResponse : BaseResponse
    {
        [DataMember(Name = "podcasts")]
        public Podcasts[] podcasts { get; set; }
    }

    [DataContract]
    public class Podcasts
    {
        [XmlElement(ElementName = "channel")]
        public Channel[] channel { get; set; }
    }

    [DataContract]
    public class Channel
    {
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }

        [XmlAttribute(AttributeName = "url")]
        public string url { get; set; }

        [XmlAttribute(AttributeName = "title")]
        public string title { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string description { get; set; }

        [XmlAttribute(AttributeName = "coverArt")]
        public string coverArt { get; set; }

        [XmlAttribute(AttributeName = "originalImageUrl")]
        public string originalImageUrl { get; set; }

        [XmlAttribute(AttributeName = "status")]
        public string status { get; set; }

        [XmlElement(ElementName = "episode")]
        public Episode[] episode { get; set; }
    }

    [DataContract]
    public class Episode
    {
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }

        [XmlAttribute(AttributeName = "streamId")]
        public string streamId { get; set; }

        [XmlAttribute(AttributeName = "channelId")]
        public string channelId { get; set; }

        [XmlAttribute(AttributeName = "title")]
        public string title { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string description { get; set; }

        [XmlAttribute(AttributeName = "publishDate")]
        public string publishDate { get; set; }

        [XmlAttribute(AttributeName = "status")]
        public string status { get; set; }

        [XmlAttribute(AttributeName = "parent")]
        public string parent { get; set; }

        [XmlAttribute(AttributeName = "isDir")]
        public string isDir { get; set; }

        [XmlAttribute(AttributeName = "album")]
        public string album { get; set; }

        [XmlAttribute(AttributeName = "artist")]
        public string artist { get; set; }

        [XmlAttribute(AttributeName = "year")]
        public string year { get; set; }

        [XmlAttribute(AttributeName = "genre")]
        public string genre { get; set; }

        [XmlAttribute(AttributeName = "coverArt")]
        public string coverArt { get; set; }

        [XmlAttribute(AttributeName = "size")]
        public string size { get; set; }

        [XmlAttribute(AttributeName = "contentType")]
        public string contentType { get; set; }

        [XmlAttribute(AttributeName = "suffix")]
        public string suffix { get; set; }

        [XmlAttribute(AttributeName = "duration")]
        public string duration { get; set; }

        [XmlAttribute(AttributeName = "bitRate")]
        public string bitRate { get; set; }

        [XmlAttribute(AttributeName = "isVideo")]
        public string isVideo { get; set; }

        [XmlAttribute(AttributeName = "created")]
        public string created { get; set; }

        [XmlAttribute(AttributeName = "artistId")]
        public string artistId { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string type { get; set; }

        [XmlAttribute(AttributeName = "path")]
        public string path { get; set; }
    }
}