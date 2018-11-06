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
    public class ArtistDirectoryXmlResponse : BaseResponse
    {
        [DataMember(Name = "directory")]
        public artistAlbumDirectory directory { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class AlbumSongDirectoryXmlResponse : BaseResponse
    {
        [DataMember(Name = "directory")]
        public albumSongDirectory directory { get; set; }
    }

    [DataContract]
    public class ArtistDirectoryJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public ArtistDirectoryJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class AlbumDirectoryJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public AlbumDirectoryJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class ArtistDirectoryJsonResponse : BaseResponse
    {
        [DataMember(Name = "directory")]
        public artistAlbumDirectory directory { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class AlbumDirectoryJsonResponse : BaseResponse
    {
        [DataMember(Name = "directory")]
        public albumSongDirectory directory { get; set; }
    }

    [DataContract]
    public class directory
    {
        [DataMember(Name = "id")]
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }
        [DataMember(Name = "name")]
        [XmlAttribute(AttributeName = "name")]
        public string name { get; set; }
        [DataMember(Name = "starred")]
        [XmlAttribute(AttributeName = "starred")]
        public string starred
        {
            get
            {
                if (this.starredDateTime.HasValue)
                {
                    return this.starredDateTime.Value.ToString("s");
                }
                return null;
            }
        }
        public DateTime? starredDateTime { get; set; }

    }

    /// <summary>
    /// Get the Albums for an Artist
    /// </summary>
    [DataContract]
    public class artistAlbumDirectory : directory
    {
        [DataMember(Name = "child")]
        [XmlElement(ElementName = "child")]
        public album[] child { get; set; }
    }

    /// <summary>
    /// Get the songs for an Album
    /// </summary>
    [DataContract]
    public class albumSongDirectory : directory
    {
        [DataMember(Name = "averageRating")]
        [XmlAttribute(AttributeName = "averageRating")]
        public decimal averageRating { get; set; }
        [DataMember(Name = "playCount")]
        [XmlAttribute(AttributeName = "playCount")]
        public int playCount { get; set; }
        [DataMember(Name = "child")]
        [XmlElement(ElementName = "child")]
        public song[] child { get; set; }
    }
}
