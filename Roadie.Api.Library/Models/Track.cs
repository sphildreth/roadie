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
        public const string DefaultIncludes = "comments,stats";

        private IEnumerable<string> _partTitles;

        [MaxLength(50)] public string AmgId { get; set; }

        public ArtistList Artist { get; set; }

        public Image ArtistThumbnail { get; set; }
        public IEnumerable<Comment> Comments { get; set; }
        public int Duration { get; set; }

        public string DurationTime
        {
            get
            {
                if (Duration < 1) return "--:--";
                return new TimeInfo(Duration).ToFullFormattedString();
            }
        }

        public long FileSize { get; set; }
        [MaxLength(32)] public string Hash { get; set; }

        [MaxLength(15)] public string ISRC { get; set; }

        [MaxLength(50)] public string LastFMId { get; set; }

        public DateTime? LastPlayed { get; set; }

        public Image MediumThumbnail { get; set; }
        [MaxLength(100)] public string MusicBrainzId { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string PartTitles { get; set; }

        public IEnumerable<string> PartTitlesList
        {
            get
            {
                if (_partTitles == null)
                {
                    if (string.IsNullOrEmpty(PartTitles)) return null;
                    return PartTitles.Replace("|", "\n").Split("\n");
                }

                return _partTitles;
            }
            set => _partTitles = value;
        }

        public int PlayedCount { get; set; }
        public short Rating { get; set; }
        public ReleaseList Release { get; set; }

        public string ReleaseMediaId { get; set; }

        public Image ReleaseThumbnail { get; set; }

        [MaxLength(100)] public string SpotifyId { get; set; }

        public TrackStatistics Statistics { get; set; }

        public Image Thumbnail { get; set; }
        [MaxLength(250)] [Required] public string Title { get; set; }

        /// <summary>
        ///     Track Artist, not release artist. If this is present then the track has an artist different than the release.
        /// </summary>
        public ArtistList TrackArtist { get; set; }

        public Image TrackArtistThumbnail { get; set; }
        public DataToken TrackArtistToken { get; set; }
        [Required] public short TrackNumber { get; set; }

        public string TrackPlayUrl { get; set; }
        public UserTrack UserRating { get; set; }

        public override string ToString()
        {
            return $"Id [{ Id }], Title [{ Title }]";
        }
    }
}