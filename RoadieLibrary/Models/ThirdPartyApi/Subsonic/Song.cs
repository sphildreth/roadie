using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    public class song
    {
        /// <summary>
        /// Database ID
        /// </summary>
        [XmlIgnore]
        public int ID { get; set; }

        [DataMember(Name = "id")]
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }

        [DataMember(Name = "parent")]
        [XmlAttribute(AttributeName = "parent")]
        public string parent { get; set; }

        [DataMember(Name = "title")]
        [XmlAttribute(AttributeName = "title")]
        public string title { get; set; }

        [DataMember(Name = "album")]
        [XmlAttribute(AttributeName = "album")]
        public string album { get; set; }

        [DataMember(Name = "artist")]
        [XmlAttribute(AttributeName = "artist")]
        public string artist { get; set; }

        [DataMember(Name = "isDir")]
        [XmlAttribute(AttributeName = "isDir")]
        public bool isDir { get; set; }

        [DataMember(Name = "coverArt")]
        [XmlAttribute(AttributeName = "coverArt")]
        public string coverArt { get; set; }

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

        [XmlIgnore]
        public DateTime? createdDateTime { get; set; }

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
            set
            {
            }
        }

        public bool ShouldSerializestarred()
        {
            return this.starredDateTime.HasValue;
        }

        [DataMember(Name = "duration")]
        [XmlAttribute(AttributeName = "duration")]
        public int duration
        {
            get
            {
                if (this.durationMilliseconds > 0)
                {
                    var contentDurationTimeSpan = TimeSpan.FromMilliseconds((double) (this.durationMilliseconds ?? 0));
                    return (int) contentDurationTimeSpan.TotalSeconds;
                }
                return 0;
            }
            set
            {
            }
        }

        [XmlIgnore]
        public int? durationMilliseconds { get; set; }

        [DataMember(Name = "bitRate")]
        [XmlAttribute(AttributeName = "bitRate")]
        public int bitRate { get; set; }

        [DataMember(Name = "track")]
        [XmlAttribute(AttributeName = "track")]
        public int track { get; set; }

        [DataMember(Name = "albumMediaNumber")]
        [XmlIgnore]
        public short? albumMediaNumber { get; set; }

        [DataMember(Name = "year")]
        [XmlAttribute(AttributeName = "year")]
        public int year
        {
            get
            {
                if (this.yearDateTime.HasValue)
                {
                    return this.yearDateTime.Value.Year;
                }
                return 0;
            }
            set
            {
            }
        }

        [XmlIgnore]
        public DateTime? yearDateTime { get; set; }

        [DataMember(Name = "genre")]
        [XmlAttribute(AttributeName = "genre")]
        public string genre { get; set; }

        [DataMember(Name = "size")]
        [XmlAttribute(AttributeName = "size")]
        public int size { get; set; }

        [DataMember(Name = "suffix")]
        [XmlAttribute(AttributeName = "suffix")]
        public string suffix { get; set; }

        [DataMember(Name = "contentType")]
        [XmlAttribute(AttributeName = "contentType")]
        public string contentType { get; set; }

        [DataMember(Name = "path")]
        [XmlAttribute(AttributeName = "path")]
        public string path { get; set; }

        [DataMember(Name = "albumId")]
        [XmlAttribute(AttributeName = "albumId")]
        public string albumId { get; set; }

        [XmlIgnore]
        public int albumID { get; set; }

        [DataMember(Name = "artistId")]
        [XmlAttribute(AttributeName = "artistId")]
        public string artistId { get; set; }

        [XmlIgnore]
        public int artistID { get; set; }

        [DataMember(Name = "type")]
        [XmlAttribute(AttributeName = "type")]
        public string type { get; set; }

        [DataMember(Name = "playCount")]
        [XmlAttribute(AttributeName = "playCount")]
        public int playCount { get; set; }

        [DataMember(Name = "userRating")]
        [XmlAttribute(AttributeName = "userRating")]
        public short userRating { get; set; }

        [DataMember(Name = "averageRating")]
        [XmlAttribute(AttributeName = "averageRating")]
        public short averageRating { get; set; }

        [XmlIgnore]
        public DateTime? starredDateTime { get; set; }

        [XmlIgnore]
        public bool isValid { get; set; }

        public song()
        {
            this.type = "music";
            this.suffix = "mp3";
            this.bitRate = 320;
            this.contentType = "audio/mpeg";
            this.isDir = false;
        }
    }
}