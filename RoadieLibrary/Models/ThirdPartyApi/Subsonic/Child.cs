using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    public class Child
    {
        [XmlAttribute(AttributeName = "id")]
        public string id { get; set; }
        [XmlAttribute(AttributeName = "parent")]
        public string parent { get; set; }
        [XmlAttribute(AttributeName = "title")]
        public string title { get; set; }
        /// <summary>
        /// True when a Album
        /// </summary>
        [XmlAttribute(AttributeName = "isDir")]
        public string isDir { get; set; }
        [XmlAttribute(AttributeName = "album")]
        public string album { get; set; }
        [XmlAttribute(AttributeName = "artist")]
        public string artist { get; set; }
        [XmlAttribute(AttributeName = "track")]
        public string track { get; set; }
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
        [XmlAttribute(AttributeName = "transcodedContentType")]
        public string transcodedContentType { get; set; }
        [XmlAttribute(AttributeName = "suffix")]
        public string suffix { get; set; }
        [XmlAttribute(AttributeName = "transcodedSuffix")]
        public string transcodedSuffix { get; set; }
        [XmlAttribute(AttributeName = "duration")]
        public string duration { get; set; }
        [XmlAttribute(AttributeName = "bitRate")]
        public string bitRate { get; set; }
        [XmlAttribute(AttributeName = "path")]
        public string path { get; set; }


        public Child()
        {
            this.isDir = "false";
        }
    }
}
