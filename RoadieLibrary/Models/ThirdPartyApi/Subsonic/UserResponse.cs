using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class UserXMLResponse : BaseResponse
    {
        [XmlElement(ElementName = "user")]
        public User user { get; set; }
    }

    public class UserJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public UserJsonResponse subsonicresponse { get; set; }
    }

    public class UserJsonResponse : BaseResponse
    {
        [DataMember(Name = "user")]
        public User user { get; set; }
    }

    public class User
    {
        /// <summary>
        /// The name of the user
        /// </summary>
        [DataMember(Name = "username")]
        [XmlAttribute(AttributeName = "username")]
        public string username { get; set; }

        /// <summary>
        /// The email address of the user.
        /// </summary>
        [DataMember(Name = "email")]
        [XmlAttribute(AttributeName = "email")]
        public string email { get; set; }

        [DataMember(Name = "scrobblingEnabled")]
        [XmlAttribute(AttributeName = "scrobblingEnabled")]
        public bool scrobblingEnabled { get; set; }

        /// <summary>
        /// Whether the user is administrator.
        /// </summary>
        [DataMember(Name = "adminRole")]
        [XmlAttribute(AttributeName = "adminRole")]
        public bool adminRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to change personal settings and password.
        /// </summary>
        [DataMember(Name = "settingsRole")]
        [XmlAttribute(AttributeName = "settingsRole")]
        public bool settingsRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to download files.
        /// </summary>
        [DataMember(Name = "downloadRole")]
        [XmlAttribute(AttributeName = "downloadRole")]
        public bool downloadRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to upload files.
        /// </summary>
        [DataMember(Name = "uploadRole")]
        [XmlAttribute(AttributeName = "uploadRole")]
        public bool uploadRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to create and delete playlists. 
        /// </summary>
        [DataMember(Name = "playlistRole")]
        [XmlAttribute(AttributeName = "playlistRole")]
        public bool playlistRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to change cover art and tags.
        /// </summary>
        [DataMember(Name = "coverArtRole")]
        [XmlAttribute(AttributeName = "coverArtRole")]
        public bool coverArtRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to create and edit comments and ratings.
        /// </summary>
        [DataMember(Name = "commentRole")]
        [XmlAttribute(AttributeName = "commentRole")]
        public bool commentRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to administrate Podcasts.
        /// </summary>
        [DataMember(Name = "podcastRole")]
        [XmlAttribute(AttributeName = "podcastRole")]
        public bool podcastRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to play files.
        /// </summary>
        [DataMember(Name = "streamRole")]
        [XmlAttribute(AttributeName = "streamRole")]
        public bool streamRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to play files in jukebox mode.
        /// </summary>
        [DataMember(Name = "jukeboxRole")]
        [XmlAttribute(AttributeName = "jukeboxRole")]
        public bool jukeboxRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to share files with anyone.
        /// </summary>
        [DataMember(Name = "shareRole")]
        [XmlAttribute(AttributeName = "shareRole")]
        public bool shareRole { get; set; }

        /// <summary>
        /// Whether the user is allowed to start video conversions.
        /// </summary>
        [DataMember(Name = "videoConversionRole")]
        [XmlAttribute(AttributeName = "videoConversionRole")]
        public bool videoConversionRole { get; set; }

        [DataMember(Name = "avatarLastChanged")]
        [XmlAttribute(AttributeName = "avatarLastChanged")]
        public DateTime avatarLastChanged { get; set; }

        /// <summary>
        /// IDs of the music folders the user is allowed access to. Include the parameter once for each folder.
        /// </summary>
        [DataMember(Name = "musicFolderId")]
        [XmlAttribute(AttributeName = "musicFolderId")]
        public int[] musicFolderId { get; set; }

        /// <summary>
        /// The maximum bit rate (in Kbps) for the user. Audio streams of higher bit rates are automatically downsampled to this bit rate. Legal values: 0 (no limit), 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320.
        /// </summary>
        [DataMember(Name = "maxBitRate")]
        [XmlAttribute(AttributeName = "maxBitRate")]
        public int? maxBitRate { get; set; }

        public User()
        {
            this.settingsRole = true;
            this.playlistRole = true;
            this.commentRole = true;
            this.streamRole = true;
            this.shareRole = true;
        }
            
    }
}