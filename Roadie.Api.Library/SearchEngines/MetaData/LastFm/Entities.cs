using System.Collections.Generic;
using System.Xml.Serialization;

namespace Roadie.Library.SearchEngines.MetaData.LastFm
{
    /// <remarks />
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class lfm
    {
        /// <remarks />
        public lfmAlbum album { get; set; }

        /// <remarks />
        [XmlAttribute]
        public string status { get; set; }
    }

    /// <remarks />
    [XmlType(AnonymousType = true)]
    public class lfmAlbum
    {
        /// <remarks />
        public string artist { get; set; }

        /// <remarks />
        [XmlElement("image")]
        public List<lfmAlbumImage> image { get; set; }

        /// <remarks />
        public ushort listeners { get; set; }

        /// <remarks />
        public string mbid { get; set; }

        /// <remarks />
        public string name { get; set; }

        /// <remarks />
        public uint playcount { get; set; }

        /// <remarks />
        [XmlArrayItem("tag", IsNullable = false)]
        public List<lfmAlbumTag> tags { get; set; }

        /// <remarks />
        [XmlArrayItem("track", IsNullable = false)]
        public List<lfmAlbumTrack> tracks { get; set; }

        /// <remarks />
        public string url { get; set; }
    }

    /// <remarks />
    [XmlType(AnonymousType = true)]
    public class lfmAlbumImage
    {
        /// <remarks />
        [XmlAttribute]
        public string size { get; set; }

        /// <remarks />
        [XmlText]
        public string Value { get; set; }
    }

    /// <remarks />
    [XmlType(AnonymousType = true)]
    public class lfmAlbumTag
    {
        /// <remarks />
        public string name { get; set; }

        /// <remarks />
        public string url { get; set; }
    }

    /// <remarks />
    [XmlType(AnonymousType = true)]
    public class lfmAlbumTrack
    {
        /// <remarks />
        public lfmAlbumTrackArtist artist { get; set; }

        /// <remarks />
        public ushort duration { get; set; }

        /// <remarks />
        public string name { get; set; }

        /// <remarks />
        [XmlAttribute]
        public byte rank { get; set; }

        /// <remarks />
        public lfmAlbumTrackStreamable streamable { get; set; }

        /// <remarks />
        public string url { get; set; }
    }

    /// <remarks />
    [XmlType(AnonymousType = true)]
    public class lfmAlbumTrackArtist
    {
        /// <remarks />
        public string mbid { get; set; }

        /// <remarks />
        public string name { get; set; }

        /// <remarks />
        public string url { get; set; }
    }

    /// <remarks />
    [XmlType(AnonymousType = true)]
    public class lfmAlbumTrackStreamable
    {
        /// <remarks />
        [XmlAttribute]
        public byte fulltrack { get; set; }

        /// <remarks />
        [XmlText]
        public byte Value { get; set; }
    }
}