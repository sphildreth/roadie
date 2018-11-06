using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    public class artist
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

        [DataMember(Name = "sortname")]
        [XmlAttribute(AttributeName = "sortname")]
        public string sortname { get; set; }

        [XmlIgnore]
        public string biography { get; set; }

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

        [DataMember(Name = "genre")]
        [XmlAttribute(AttributeName = "genre")]
        public string genre { get; set; }

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

        [DataMember(Name = "coverArt")]
        [XmlAttribute(AttributeName = "coverArt")]
        public string coverArt { get; set; }

        [DataMember(Name = "albumCount")]
        [XmlAttribute(AttributeName = "albumCount")]
        public int albumCount { get; set; }

        [DataMember(Name = "userRating")]
        [XmlAttribute(AttributeName = "userRating")]
        public short userRating { get; set; }

        [XmlIgnore]
        public DateTime? starredDateTime { get; set; }
        [XmlIgnore]
        public DateTime? createdDateTime { get; set; }

        [DataMember(Name = "album")]
        [XmlElement(ElementName = "album")]
        public album[] album { get; set; }

        public bool ShouldSerializealbum()
        {
            return this.album != null;
        }
    }
}