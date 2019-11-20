using System;
using System.Collections.Generic;
using System.IO;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public sealed class RoadieSettings : IRoadieSettings
    {
        public DbContexts DbContextToUse { get; set; }
         
        /// <summary>
        ///     If the artist name is found in the values then use the key.
        ///     <remark>This was desgined to handle 'AC/DC' type names as they contain the ID3 v2.3 spec artist seperator</remark>
        /// </summary>
        public Dictionary<string, IEnumerable<string>> ArtistNameReplace { get; set; }

        public string BehindProxyHost { get; set; }

        public string CollectionImageFolder
        {
            get
            {
                return Path.Combine(ImageFolder ?? LibraryFolder, "__roadie_images", "collections");
            }
        }

        /// <summary>
        ///     Set to the Roadie Database for DbDataReader operations
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     This is the phsycial path to the content folder (which holds among other things place-holder images)
        /// </summary>
        public string ContentPath { get; set; }

        public Converting Converting { get; set; }

        public FileDatabaseOptions FileDatabaseOptions { get; set; }

        public short DefaultRowsPerPage { get; set; }
        public string DefaultTimeZone { get; set; }

        public Dlna Dlna { get; set; }

        public IEnumerable<string> DontDoMetaDataProvidersSearchArtists { get; set; }

        public IEnumerable<string> FileExtensionsToDelete { get; set; }

        public FilePlugins FilePlugins { get; set; }

        public string GenreImageFolder
        {
            get
            {
                return Path.Combine(ImageFolder ?? LibraryFolder, "__roadie_images", "genres");
            }
        }

        public string ImageFolder { get; set; }
        public string InboundFolder { get; set; }

        public Inspector Inspector { get; set; }

        public Integrations Integrations { get; set; }

        /// <summary>
        /// If true then don't allow new registrations
        /// </summary>
        public bool IsRegistrationClosed { get; set; }

        public string LabelImageFolder
        {
            get
            {
                return Path.Combine(ImageFolder ?? LibraryFolder, "__roadie_images", "labels");
            }
        }

        public ImageSize LargeImageSize { get; set; }

        public string LibraryFolder { get; set; }
        public string ListenAddress { get; set; }

        public ImageSize MaximumImageSize { get; set; }

        public ImageSize MediumImageSize { get; set; }

        public string PlaylistImageFolder
        {
            get
            {
                return Path.Combine(ImageFolder ?? LibraryFolder, "__roadie_images", "playlists");
            }
        }

        public Processing Processing { get; set; }

        public bool RecordNoResultSearches { get; set; }

        public RedisCache Redis { get; set; }

        /// <summary>
        /// Place to hold cache repositories used by SearchEngine and MetaData engines
        /// </summary>
        public string SearchEngineReposFolder { get; set; }

        public string SecretKey { get; set; }

        public string SiteName { get; set; }

        public ImageSize SmallImageSize { get; set; }

        public string SmtpFromAddress { get; set; }

        public string SmtpHost { get; set; }

        public string SmtpPassword { get; set; }

        public int SmtpPort { get; set; }

        public string SmtpUsername { get; set; }

        public bool SmtpUseSSl { get; set; }

        public short? SubsonicRatingBoost { get; set; }

        public ImageSize ThumbnailImageSize { get; set; }

        public Dictionary<string, string> TrackPathReplace { get; set; }

        /// <summary>
        /// When true require a "invite" token to exist for a user to register.
        /// </summary>
        public bool UseRegistrationTokens { get; set; }

        public string UserImageFolder
        {
            get
            {
                return Path.Combine(LibraryFolder, "__roadie_images", "users");
            }
        }

        public bool UseSSLBehindProxy { get; set; }

        public string WebsocketAddress { get; set; }

        public RoadieSettings()
        {
            DbContextToUse = DbContexts.MySQL;
            ArtistNameReplace = new Dictionary<string, IEnumerable<string>>
            {
                { "AC/DC", new List<string>{ "AC; DC", "AC;DC", "AC/ DC", "AC DC" }},
                { "Love/Hate", new List<string>{ "Love; Hate", "Love;Hate", "Love/ Hate", "Love Hate" }}
            };
            DefaultTimeZone = "US / Central";
            DontDoMetaDataProvidersSearchArtists = new List<string> { "Various Artists", "Sound Tracks" };
            FileExtensionsToDelete = new List<string> { ".accurip", ".bmp", ".cue", ".dat", ".db", ".exe", ".gif", ".htm", ".html", ".ini", ".log", ".jpg", ".jpeg", ".par", ".par2", ".pdf", ".png", ".md5", ".mht", ".mpg", ".m3u", ".nfo", ".nzb", ".pls", ".sfv", ".srr", ".txt", ".url" };
            InboundFolder = "M:/inbound";
            LargeImageSize = new ImageSize { Width = 500, Height = 500 };
            LibraryFolder = "M:/library";
            MaximumImageSize = new ImageSize { Width = 2048, Height = 2048 };
            MediumImageSize = new ImageSize { Width = 320, Height = 320 };
            RecordNoResultSearches = true;
            SiteName = "Roadie";
            SmallImageSize = new ImageSize { Width = 160, Height = 160 };
            ThumbnailImageSize = new ImageSize { Width = 80, Height = 80 };
            DefaultRowsPerPage = 12;

            SmtpFromAddress = "noreply@roadie.rocks";
            SmtpPort = 587;
            SmtpUsername = "roadie";
            SmtpHost = "smtp.roadie.rocks";
            SmtpUseSSl = true;

            Inspector = new Inspector();
            Converting = new Converting();
            FileDatabaseOptions = new FileDatabaseOptions();
            Integrations = new Integrations();
            Processing = new Processing();
            Dlna = new Dlna();

        }
    }
}