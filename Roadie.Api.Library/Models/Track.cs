using Newtonsoft.Json;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Track : EntityModelBase
    {
        public const string DefaultIncludes = "stats";

        [MaxLength(50)]
        public string AmgId { get; set; }

        public DataToken Artist { get; set; }

        public Image ArtistThumbnail { get; set; }
        public long FileSize { get; set; }
        public int Duration { get; set; }

        [MaxLength(32)]
        public string Hash { get; set; }

        [MaxLength(15)]
        public string ISRC { get; set; }

        [MaxLength(50)]
        public string LastFMId { get; set; }

        public DateTime? LastPlayed { get; set; }

        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [MaxLength(65535)]
        public string PartTitles { get; set; }

        public int PlayedCount { get; set; }
        public short Rating { get; set; }
        public DataToken Release { get; set; }

        public string ReleaseMediaId { get; set; }

        public Image ReleaseThumbnail { get; set; }

        [MaxLength(100)]
        public string SpotifyId { get; set; }

        public TrackStatistics Statistics { get; set; }

        public Image Thumbnail { get; set; }

        [MaxLength(250)]
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Track Artist, not release artist. If this is present then the track has an artist different than the release.
        /// </summary>
        public DataToken TrackArtist { get; set; }

        public Image TrackArtistThumbnail { get; set; }

        [Required]
        public short TrackNumber { get; set; }

        public UserTrack UserRating { get; set; }

        public string PlayUrl { get; set; }
    }
}