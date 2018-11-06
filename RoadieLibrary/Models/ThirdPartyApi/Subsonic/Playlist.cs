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
    public class PlaylistsXMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "playlists")]
        public Playlists playlists { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class PlaylistXMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "playlist")]
        public Playlist playlist { get; set; }
    }

    [DataContract]
    public class PlaylistsJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public PlaylistsJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class PlaylistJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public PlaylistJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class PlaylistsJsonResponse : BaseResponse
    {
        [DataMember(Name = "playlists")]
        public Playlists playlists { get; set; }
    }

    [DataContract]
    public class PlaylistJsonResponse : BaseResponse
    {
        [DataMember(Name = "playlist")]
        public Playlist playlist { get; set; }
    }

    [DataContract]
    public class Playlists
    {
        [DataMember(Name = "playlist")]
        [XmlElement(ElementName = "playlist")]
        public Playlist[] playlist { get; set; }
    }

    [DataContract]
    public class Playlist
    {
        /// <summary>
        /// Database ID 
        /// </summary>
        [XmlIgnore]
        public int ID { get; set; }

        [DataMember(Name = "id")]
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }
        [DataMember(Name = "name")]
        [XmlAttribute(AttributeName = "name")]
        public string name { get; set; }
        [DataMember(Name = "comment")]
        [XmlAttribute(AttributeName = "comment")]
        public string comment { get; set; }
        [DataMember(Name = "owner")]
        [XmlAttribute(AttributeName = "owner")]
        public string owner { get; set; }
        [DataMember(Name = "isPublic")]
        [XmlAttribute(AttributeName = "public")]
        public bool isPublic { get; set; }
        [DataMember(Name = "songCount")]
        [XmlAttribute(AttributeName = "songCount")]
        public int songCount { get; set; }
        [DataMember(Name = "duration")]
        [XmlAttribute(AttributeName = "duration")]
        public int duration
        {
            get
            {
                var contentDurationTimeSpan = TimeSpan.FromMilliseconds((double) (durationMilliseconds));
                return (int) contentDurationTimeSpan.TotalSeconds;
            }
        }

        public int durationMilliseconds { get; set; }
        [DataMember(Name = "created")]
        [XmlAttribute(AttributeName = "created")]
        public string created
        {
            get
            {
                if (this.createdDateTime.HasValue)
                {
                    return this.createdDateTime.Value.ToString("s");
                }
                return null;
            }
            set
            {
            }
        }
        public DateTime? createdDateTime { get; set; }
        [DataMember(Name = "changed")]
        [XmlAttribute(AttributeName = "changed")]
        public string changed
        {
            get
            {
                if (this.changedDateTime.HasValue)
                {
                    return this.changedDateTime.Value.ToString("s");
                }
                return null;
            }
            set
            {
            }
        }
        public DateTime? changedDateTime { get; set; }
        [DataMember(Name = "coverArt")]
        [XmlAttribute(AttributeName = "coverArt")]
        public string coverArt { get; set; }
        [DataMember(Name = "allowedUser")]
        [XmlElement(ElementName = "allowedUser")]
        public string[] allowedUser { get; set; }
        [DataMember(Name = "entry")]
        [XmlElement(ElementName = "entry")]
        public song[] entry { get; set; }

        public Playlist()
        {
            this.allowedUser = new string[0];
            this.entry = new song[0];
        }
    }




}
