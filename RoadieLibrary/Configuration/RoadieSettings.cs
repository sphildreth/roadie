using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roadie.Library.Setttings
{
    [Serializable]
    public sealed class RoadieSettings : IRoadieSettings
    {
        public bool UseSSLBehindProxy { get; set; }
        public string SecretKey { get; set; }
        public string WebsocketAddress { get; set; }
        public string SiteName { get; set; }
        public string InboundFolder { get; set; }
        public string LibraryFolder { get; set; }
        public Dictionary<string, string> TrackPathReplace { get; set; }
        public string SingleArtistHoldingFolder { get; set; }
        public string DefaultTimeZone { get; set; }
        public Thumbnails Thumbnails { get; set; }
        public List<ApiKey> ApiKeys { get; set; }
        public Converting Converting { get; set; }
        public Processing Processing { get; set; }
        public Integrations Integrations { get; set; }
        public string DiagnosticsPassword { get; set; }

        public string SmtpFromAddress { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpHost { get; set; }
        public bool SmtpUseSSl { get; set; }

        public IEnumerable<string> DontDoMetaDataProvidersSearchArtists { get; set; }
        public IEnumerable<string> FileExtensionsToDelete { get; set; }

        /// <summary>
        /// If the artist name is found in the values then use the key.
        /// <remark>This was desgined to handle 'AC/DC' type names as they contain the ID3 v2.3 spec artist seperator</remark>
        /// </summary>
        public Dictionary<string, List<string>> ArtistNameReplace { get; set; }

        public bool RecordNoResultSearches { get; set; }

        public RedisCache Redis { get; set; }

        public RoadieSettings()
        {
            this.SiteName = "Roadie";
            this.DefaultTimeZone = "US/Central";
            this.DiagnosticsPassword = this.SiteName;
            this.WebsocketAddress = "ws://localhost:3579/websocket";
            this.TrackPathReplace = new Dictionary<string, string>();
            this.Thumbnails = new Thumbnails();
            this.Converting = new Converting();
            this.Processing = new Processing();
            this.Integrations = new Integrations();
            this.ApiKeys = new List<ApiKey>();
            this.DontDoMetaDataProvidersSearchArtists = new string[] { "Various Artists", "Sound Tracks" };
            this.FileExtensionsToDelete = new string[] { ".cue", ".db", ".gif", ".html", ".ini", ".jpg", ".jpeg", ".log", ".mpg", ".m3u", ".png", ".nfo", ".nzb", ".sfv", ".srr", ".txt", ".url" };
            this.ArtistNameReplace = new Dictionary<string, List<string>>();
            this.RecordNoResultSearches = true;

        }

        public void MergeWith(RoadieSettings secondary)
        {
            foreach (var pi in typeof(RoadieSettings).GetProperties())
            {
                var priValue = pi.GetGetMethod().Invoke(this, null);
                var secValue = pi.GetGetMethod().Invoke(secondary, null);
                if (secValue != null && pi.CanWrite)
                {
                    pi.GetSetMethod().Invoke(this, new object[] { secValue });
                }
            }
        }
    }
}
