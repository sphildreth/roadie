using System;
using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public sealed class RoadieSettings : IRoadieSettings
    {
        /// <summary>
        ///     If the artist name is found in the values then use the key.
        ///     <remark>This was desgined to handle 'AC/DC' type names as they contain the ID3 v2.3 spec artist seperator</remark>
        /// </summary>
        public Dictionary<string, IEnumerable<string>> ArtistNameReplace { get; set; }

        public string BehindProxyHost { get; set; }

        /// <summary>
        ///     Set to the Roadie Database for DbDataReader operations
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     This is the phsycial path to the content folder (which holds among other things place-holder images)
        /// </summary>
        public string ContentPath { get; set; }

        public Converting Converting { get; set; }

        public string DefaultTimeZone { get; set; }

        public IEnumerable<string> DontDoMetaDataProvidersSearchArtists { get; set; }

        public IEnumerable<string> FileExtensionsToDelete { get; set; }

        public FilePlugins FilePlugins { get; set; }

        public string InboundFolder { get; set; }

        public Inspector Inspector { get; set; }

        public Integrations Integrations { get; set; }

        public ImageSize LargeImageSize { get; set; }

        public string LibraryFolder { get; set; }

        public string ListenAddress { get; set; }

        public ImageSize MaximumImageSize { get; set; }

        public ImageSize MediumImageSize { get; set; }

        public Processing Processing { get; set; }

        public bool RecordNoResultSearches { get; set; }

        public RedisCache Redis { get; set; }

        public string SecretKey { get; set; }

        public string SiteName { get; set; }

        public ImageSize SmallImageSize { get; set; }

        public string SmtpFromAddress { get; set; }

        public string SmtpHost { get; set; }

        public string SmtpPassword { get; set; }

        public int SmtpPort { get; set; }

        public string SmtpUsername { get; set; }

        public bool SmtpUseSSl { get; set; }

        public ImageSize ThumbnailImageSize { get; set; }

        public Dictionary<string, string> TrackPathReplace { get; set; }

        public bool UseSSLBehindProxy { get; set; }

        public string WebsocketAddress { get; set; }

        public short? SubsonicRatingBoost { get; set; }

        /// <summary>
        /// When true require a "invite" token to exist for a user to register.
        /// </summary>
        public bool UseRegistrationTokens { get; set; }

        /// <summary>
        /// If true then don't allow new registrations
        /// </summary>
        public bool IsRegistrationClosed { get; set; }

        public RoadieSettings()
        {
            ArtistNameReplace = new Dictionary<string, IEnumerable<string>>
            {
                { "AC/DC", new List<string>{ "AC; DC", "AC;DC", "AC/ DC", "AC DC" }},
                { "Love/Hate", new List<string>{ "Love; Hate", "Love;Hate", "Love/ Hate", "Love Hate" }}
            };
            DefaultTimeZone = "US / Central";
            DontDoMetaDataProvidersSearchArtists = new List<string> { "Various Artists", "Sound Tracks" };
            FileExtensionsToDelete = new List<string>{ ".accurip", ".bmp", ".cue", ".dat", ".db", ".exe", ".htm", ".html", ".ini", ".log", ".jpg", ".jpeg", ".par", ".par2", ".pdf", ".png", ".md5", ".mht", ".mpg", ".m3u", ".nfo", ".nzb", ".pls", ".sfv", ".srr", ".txt", ".url" };
            InboundFolder = "M:/inbound";
            LargeImageSize = new ImageSize { Width = 500, Height = 500 };
            LibraryFolder = "M:/library";
            MaximumImageSize = new ImageSize { Width = 2048, Height = 2048 };
            MediumImageSize = new ImageSize { Width = 320, Height = 320 };
            RecordNoResultSearches = true;
            SiteName = "Roadie";
            SmallImageSize = new ImageSize { Width = 160, Height = 160 };
            ThumbnailImageSize = new ImageSize { Width = 80, Height = 80 };

            SmtpFromAddress = "noreply@roadie.rocks";
            SmtpPort = 587;
            SmtpUsername = "roadie";
            SmtpHost = "smtp.roadie.rocks";
            SmtpUseSSl = true;

            Inspector = new Inspector();
            Converting = new Converting();
            Integrations = new Integrations();
            Processing = new Processing();
        }
    }
}