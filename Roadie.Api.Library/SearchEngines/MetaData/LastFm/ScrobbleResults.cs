using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.SearchEngines.MetaData.LastFm
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class scrobbles
    {

        private scrobblesScrobble scrobbleField;

        private byte ignoredField;

        private byte acceptedField;

        /// <remarks/>
        public scrobblesScrobble scrobble
        {
            get
            {
                return this.scrobbleField;
            }
            set
            {
                this.scrobbleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte ignored
        {
            get
            {
                return this.ignoredField;
            }
            set
            {
                this.ignoredField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte accepted
        {
            get
            {
                return this.acceptedField;
            }
            set
            {
                this.acceptedField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class scrobblesScrobble
    {

        private scrobblesScrobbleTrack trackField;

        private scrobblesScrobbleArtist artistField;

        private scrobblesScrobbleAlbum albumField;

        private scrobblesScrobbleAlbumArtist albumArtistField;

        private uint timestampField;

        private scrobblesScrobbleIgnoredMessage ignoredMessageField;

        /// <remarks/>
        public scrobblesScrobbleTrack track
        {
            get
            {
                return this.trackField;
            }
            set
            {
                this.trackField = value;
            }
        }

        /// <remarks/>
        public scrobblesScrobbleArtist artist
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
        public scrobblesScrobbleAlbum album
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
        public scrobblesScrobbleAlbumArtist albumArtist
        {
            get
            {
                return this.albumArtistField;
            }
            set
            {
                this.albumArtistField = value;
            }
        }

        /// <remarks/>
        public uint timestamp
        {
            get
            {
                return this.timestampField;
            }
            set
            {
                this.timestampField = value;
            }
        }

        /// <remarks/>
        public scrobblesScrobbleIgnoredMessage ignoredMessage
        {
            get
            {
                return this.ignoredMessageField;
            }
            set
            {
                this.ignoredMessageField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class scrobblesScrobbleTrack
    {

        private byte correctedField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte corrected
        {
            get
            {
                return this.correctedField;
            }
            set
            {
                this.correctedField = value;
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
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class scrobblesScrobbleArtist
    {

        private byte correctedField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte corrected
        {
            get
            {
                return this.correctedField;
            }
            set
            {
                this.correctedField = value;
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
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class scrobblesScrobbleAlbum
    {

        private byte correctedField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte corrected
        {
            get
            {
                return this.correctedField;
            }
            set
            {
                this.correctedField = value;
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
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class scrobblesScrobbleAlbumArtist
    {

        private byte correctedField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte corrected
        {
            get
            {
                return this.correctedField;
            }
            set
            {
                this.correctedField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class scrobblesScrobbleIgnoredMessage
    {

        private byte codeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte code
        {
            get
            {
                return this.codeField;
            }
            set
            {
                this.codeField = value;
            }
        }
    }


}
