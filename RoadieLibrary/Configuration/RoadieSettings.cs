using System;
using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public sealed class RoadieSettings : IRoadieSettings
    {
        /// <summary>
        /// If the artist name is found in the values then use the key.
        /// <remark>This was desgined to handle 'AC/DC' type names as they contain the ID3 v2.3 spec artist seperator</remark>
        /// </summary>
        public Dictionary<string, List<string>> ArtistNameReplace { get; set; }
        /// <summary>
        /// This is the phsycial path to the content folder (which holds among other things place-holder images)
        /// </summary>
        public string ContentPath { get; set; }
        public Converting Converting { get; set; }
        public string DefaultTimeZone { get; set; }
        public string DiagnosticsPassword { get; set; }
        public IEnumerable<string> DontDoMetaDataProvidersSearchArtists { get; set; }
        public IEnumerable<string> FileExtensionsToDelete { get; set; }
        public FilePlugins FilePlugins { get; set; }
        public string InboundFolder { get; set; }
        public Integrations Integrations { get; set; }
        public Thumbnails LargeThumbnails { get; set; }
        public string LibraryFolder { get; set; }
        public string ListenAddress { get; set; }
        public Thumbnails MediumThumbnails { get; set; }
        public Processing Processing { get; set; }
        public bool RecordNoResultSearches { get; set; }
        public RedisCache Redis { get; set; }
        public string SecretKey { get; set; }
        public string SingleArtistHoldingFolder { get; set; }
        public string SiteName { get; set; }
        public string SmtpFromAddress { get; set; }
        public string SmtpHost { get; set; }
        public string SmtpPassword { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public bool SmtpUseSSl { get; set; }
        public Thumbnails Thumbnails { get; set; }
        public Dictionary<string, string> TrackPathReplace { get; set; }
        public bool UseSSLBehindProxy { get; set; }
        public string WebsocketAddress { get; set; }
    }
}