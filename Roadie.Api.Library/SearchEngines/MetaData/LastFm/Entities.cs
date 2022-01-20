namespace Roadie.Library.SearchEngines.MetaData.LastFm
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class lfm
    {
        private lfmAlbum albumField;

        private string statusField;

        /// <remarks/>
        public lfmAlbum album
        {
            get
            {
                return this.albumField;
            }
            set
            {
                this.albumField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class lfmAlbum
    {
        private string nameField;

        private string artistField;

        private string mbidField;

        private string urlField;

        private lfmAlbumImage[] imageField;

        private uint? listenersField;

        private uint? playcountField;

        private lfmAlbumTrack[] tracksField;

        private lfmAlbumTag[] tagsField;

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string artist
        {
            get
            {
                return this.artistField;
            }
            set
            {
                this.artistField = value;
            }
        }

        /// <remarks/>
        public string mbid
        {
            get
            {
                return this.mbidField;
            }
            set
            {
                this.mbidField = value;
            }
        }

        /// <remarks/>
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("image")]
        public lfmAlbumImage[] image
        {
            get
            {
                return this.imageField;
            }
            set
            {
                this.imageField = value;
            }
        }

        /// <remarks/>
        public uint? listeners
        {
            get
            {
                return this.listenersField;
            }
            set
            {
                this.listenersField = value;
            }
        }

        /// <remarks/>
        public uint? playcount
        {
            get
            {
                return this.playcountField;
            }
            set
            {
                this.playcountField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("track", IsNullable = false)]
        public lfmAlbumTrack[] tracks
        {
            get
            {
                return this.tracksField;
            }
            set
            {
                this.tracksField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("tag", IsNullable = false)]
        public lfmAlbumTag[] tags
        {
            get
            {
                return this.tagsField;
            }
            set
            {
                this.tagsField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class lfmAlbumImage
    {
        private string sizeField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string size
        {
            get
            {
                return this.sizeField;
            }
            set
            {
                this.sizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class lfmAlbumTrack
    {
        private string nameField;

        private string urlField;

        private ushort durationField;

        private lfmAlbumTrackStreamable streamableField;

        private lfmAlbumTrackArtist artistField;

        private byte rankField;

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }

        /// <remarks/>
        public ushort duration
        {
            get
            {
                return this.durationField;
            }
            set
            {
                this.durationField = value;
            }
        }

        /// <remarks/>
        public lfmAlbumTrackStreamable streamable
        {
            get
            {
                return this.streamableField;
            }
            set
            {
                this.streamableField = value;
            }
        }

        /// <remarks/>
        public lfmAlbumTrackArtist artist
        {
            get
            {
                return this.artistField;
            }
            set
            {
                this.artistField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte rank
        {
            get
            {
                return this.rankField;
            }
            set
            {
                this.rankField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class lfmAlbumTrackStreamable
    {
        private byte fulltrackField;

        private byte valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte fulltrack
        {
            get
            {
                return this.fulltrackField;
            }
            set
            {
                this.fulltrackField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public byte Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class lfmAlbumTrackArtist
    {
        private string nameField;

        private string mbidField;

        private string urlField;

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string mbid
        {
            get
            {
                return this.mbidField;
            }
            set
            {
                this.mbidField = value;
            }
        }

        /// <remarks/>
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class lfmAlbumTag
    {
        private string nameField;

        private string urlField;

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }
    }
}
