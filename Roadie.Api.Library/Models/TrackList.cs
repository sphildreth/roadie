using Newtonsoft.Json;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using Release = Roadie.Library.Data.Release;

namespace Roadie.Library.Models
{
    [Serializable]
    [DebuggerDisplay("Trackid [{ TrackId }], Track Name [{ TrackName }}")]
    public class TrackList : EntityInfoModelBase
    {
        public ArtistList Artist { get; set; }
        public int? Duration { get; set; }

        public string DurationTime =>
            Duration.HasValue ? new TimeInfo(Duration.Value).ToFullFormattedString() : "--:--";

        public string DurationTimeShort =>
            Duration.HasValue ? new TimeInfo(Duration.Value).ToShortFormattedString() : "--:--";

        public int? FavoriteCount { get; set; }
        public int? FileSize { get; set; }
        public DateTime? LastPlayed { get; set; }
        public int? MediaNumber { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string PartTitles { get; set; }

        public IEnumerable<string> PartTitlesList
        {
            get
            {
                if (string.IsNullOrEmpty(PartTitles)) return null;
                return PartTitles.Split('\n');
            }
        }

        public int? PlayedCount { get; set; }
        public short? Rating { get; set; }
        public ReleaseList Release { get; set; }
        [JsonIgnore] public DateTime? ReleaseDate { get; set; }
        public short? ReleaseRating { get; set; }
        public Image Thumbnail { get; set; }
        public string Title { get; set; }
        public DataToken Track { get; set; }
        public ArtistList TrackArtist { get; set; }
        [JsonIgnore] public string TrackId => Track?.Value;

        [JsonIgnore] public string TrackName => Track?.Text;
        public int? TrackNumber { get; set; }
        public string TrackPlayUrl { get; set; }
        public UserTrack UserRating { get; set; }

        public int? Year
        {
            get
            {
                if (ReleaseDate.HasValue) return ReleaseDate.Value.Year;
                return null;
            }
        }

        public static TrackList FromDataTrack(string trackPlayUrl,
            Data.Track track,
            int releaseMediaNumber,
            Release release,
            Data.Artist artist,
            Data.Artist trackArtist,
            string baseUrl,
            Image trackThumbnail,
            Image releaseThumbnail,
            Image artistThumbnail,
            Image trackArtistThumbnail)
        {
            return new TrackList
            {
                DatabaseId = track.Id,
                Id = track.RoadieId,
                Track = new DataToken
                {
                    Text = track.Title,
                    Value = track.RoadieId.ToString()
                },
                Release = ReleaseList.FromDataRelease(release, artist, baseUrl, artistThumbnail, releaseThumbnail),
                LastPlayed = track.LastPlayed,
                Artist = ArtistList.FromDataArtist(artist, artistThumbnail),
                TrackArtist = trackArtist == null ? null : ArtistList.FromDataArtist(trackArtist, trackArtistThumbnail),
                TrackNumber = track.TrackNumber,
                MediaNumber = releaseMediaNumber,
                CreatedDate = track.CreatedDate,
                LastUpdated = track.LastUpdated,
                Duration = track.Duration,
                FileSize = track.FileSize,
                ReleaseDate = release.ReleaseDate,
                PlayedCount = track.PlayedCount,
                Rating = track.Rating,
                Title = track.Title,
                TrackPlayUrl = trackPlayUrl,
                Thumbnail = trackThumbnail
            };
        }
    }
}