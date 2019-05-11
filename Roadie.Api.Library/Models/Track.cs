using Newtonsoft.Json;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Track : EntityModelBase
    {
        public const string DefaultIncludes = "stats";

        [MaxLength(50)]
        public string AmgId { get; set; }

        public ArtistList Artist { get; set; }

        public Image ArtistThumbnail { get; set; }
        public long FileSize { get; set; }
        public int Duration { get; set; }
        public string DurationTime
        {
            get
            {
                if (this.Duration < 1)
                {
                    return "--:--";
                }
                return new TimeInfo(this.Duration).ToFullFormattedString();
            }
        }

        [MaxLength(32)]
        public string Hash { get; set; }

        [MaxLength(15)]
        public string ISRC { get; set; }

        [MaxLength(50)]
        public string LastFMId { get; set; }

        public DateTime? LastPlayed { get; set; }

        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string PartTitles { get; set; }

        private IEnumerable<string> _partTitles = null;
        public IEnumerable<string> PartTitlesList
        {
            get
            {
                if (this._partTitles == null)
                {
                    if (string.IsNullOrEmpty(this.PartTitles))
                    {
                        return null;
                    }
                    return this.PartTitles.Replace("|", "\n").Split("\n");
                }
                return this._partTitles;
            }
            set
            {
                this._partTitles = value;
            }
        }

        public int PlayedCount { get; set; }
        public short Rating { get; set; }
        public ReleaseList Release { get; set; }

        public string ReleaseMediaId { get; set; }

        public Image ReleaseThumbnail { get; set; }

        [MaxLength(100)]
        public string SpotifyId { get; set; }

        public TrackStatistics Statistics { get; set; }

        public Image Thumbnail { get; set; }
        public Image MediumThumbnail { get; set; }

        [MaxLength(250)]
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Track Artist, not release artist. If this is present then the track has an artist different than the release.
        /// </summary>
        public ArtistList TrackArtist { get; set; }

        public Image TrackArtistThumbnail { get; set; }

        [Required]
        public short TrackNumber { get; set; }

        public UserTrack UserRating { get; set; }

        public string TrackPlayUrl { get; set; }
    }
}