using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    [DebuggerDisplay("Title [{title}], PlayCount [{playCount}]")]
    public class album
    {
        /// <summary>
        /// Database ID
        /// </summary>
        [XmlIgnore]
        public int ID { get; set; }

        [DataMember(Name = "id")]
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }

        [DataMember(Name = "userRating")]
        [XmlAttribute(AttributeName = "userRating")]
        public short userRating { get; set; }

        [DataMember(Name = "averageRating")]
        [XmlAttribute(AttributeName = "averageRating")]
        public short averageRating { get; set; }

        [DataMember(Name = "playCount")]
        [XmlAttribute(AttributeName = "playCount")]
        public int playCount { get; set; }

        [DataMember(Name = "parent")]
        [XmlAttribute(AttributeName = "parent")]
        public string parent { get; set; }

        [DataMember(Name = "title")]
        [XmlAttribute(AttributeName = "title")]
        public string title { get; set; }

        [DataMember(Name = "genre")]
        [XmlAttribute(AttributeName = "genre")]
        public string genre { get; set; }

        [DataMember(Name = "album")]
        [XmlAttribute(AttributeName = "album")]
        public string albumTitle
        {
            get
            {
                return this.title;
            }
        }

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

        [DataMember(Name = "isDir")]
        [XmlAttribute(AttributeName = "isDir")]
        public bool isDir { get; set; }

        [DataMember(Name = "coverArt")]
        [XmlAttribute(AttributeName = "coverArt")]
        public string coverArt { get; set; }

        [DataMember(Name = "songCount")]
        [XmlAttribute(AttributeName = "songCount")]
        public int songCount { get; set; }

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

        [XmlIgnore]
        public DateTime? lastPlayed { get; set; }

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

        [XmlIgnore]
        public DateTime? starredDateTime { get; set; }

        [DataMember(Name = "artist")]
        [XmlAttribute(AttributeName = "artist")]
        public string artist { get; set; }

        [DataMember(Name = "artistId")]
        [XmlAttribute(AttributeName = "artistId")]
        public string artistId { get; set; }

        [XmlIgnore]
        public int artistID { get; set; }

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

        [XmlIgnore]
        public int? listNumber { get; set; }

        [DataMember(Name = "song")]
        [XmlElement(ElementName = "song")]
        public song[] song { get; set; }

        public bool ShouldSerializesong()
        {
            return this.song != null;
        }

        [XmlIgnore]
        public string notes { get; set; }

        [XmlIgnore]
        public string musicBrainzId { get; set; }

        [XmlIgnore]
        public string lastFMUrl { get; set; }

        [XmlIgnore]
        public string smallImageUrl { get; set; }

        [XmlIgnore]
        public string mediumImageUrl { get; set; }

        [XmlIgnore]
        public string largeImageUrl { get; set; }
    }
}