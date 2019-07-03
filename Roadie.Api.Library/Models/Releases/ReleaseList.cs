using Mapster;
using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    [DebuggerDisplay("DatabaseId [{ DatabaseId }] Name [{ ReleaseName }] IsValid [{ IsValid }]")]
    public class ReleaseList : EntityInfoModelBase
    {
        public DataToken Artist { get; set; }
        public Image ArtistThumbnail { get; set; }
        public decimal? Duration { get; set; }

        public string DurationTime
        {
            get
            {
                if (!Duration.HasValue) return "--:--";
                return new TimeInfo(Duration.Value).ToFullFormattedString();
            }
        }

        public DataToken Genre { get; set; }

        public bool IsValid
        {
            get
            {
                var artistName = Artist?.Text;
                var releaseName = Release?.Text;
                return Id != Guid.Empty &&
                       !string.IsNullOrEmpty(artistName) &&
                       !string.IsNullOrEmpty(releaseName);
            }
        }

        public DateTime? LastPlayed { get; set; }
        public LibraryStatus? LibraryStatus { get; set; }

        /// <summary>
        ///     This is populated on a request to get the list of releases for a collection.
        /// </summary>
        public int? ListNumber { get; set; }

        public IEnumerable<ReleaseMediaList> Media { get; set; }
        public int? MediaCount { get; set; }
        public decimal? Rank { get; set; }
        public short? Rating { get; set; }
        public DataToken Release { get; set; }

        public string ReleaseDate => ReleaseDateDateTime.HasValue
            ? ReleaseDateDateTime.Value.ToUniversalTime().ToString("yyyy-MM-dd")
            : null;

        [JsonIgnore] public DateTime? ReleaseDateDateTime { get; set; }
        [JsonIgnore] [AdaptIgnore] public string ReleaseName => Release?.Text;
        public string ReleasePlayUrl { get; set; }

        public string ReleaseYear => ReleaseDateDateTime.HasValue
            ? ReleaseDateDateTime.Value.ToUniversalTime().ToString("yyyy")
            : null;

        public Statuses? Status { get; set; }
        public Image Thumbnail { get; set; }
        public int? TrackCount { get; set; }
        public int? TrackPlayedCount { get; set; }
        public UserRelease UserRating { get; set; }

        public static ReleaseList FromDataRelease(Data.Release release, Data.Artist artist, string baseUrl,
            Image artistThumbnail, Image thumbnail)
        {
            return new ReleaseList
            {
                DatabaseId = release.Id,
                Id = release.RoadieId,
                Artist = new DataToken
                {
                    Value = artist.RoadieId.ToString(),
                    Text = artist.Name
                },
                Release = new DataToken
                {
                    Text = release.Title,
                    Value = release.RoadieId.ToString()
                },
                ArtistThumbnail = artistThumbnail,
                CreatedDate = release.CreatedDate,
                Duration = release.Duration,
                LastPlayed = release.LastPlayed,
                LastUpdated = release.LastUpdated,
                LibraryStatus = release.LibraryStatus,
                MediaCount = release.MediaCount,
                Rating = release.Rating,
                Rank = release.Rank,
                ReleaseDateDateTime = release.ReleaseDate,
                ReleasePlayUrl = $"{baseUrl}/play/release/{release.RoadieId}",
                Status = release.Status,
                Thumbnail = thumbnail,
                TrackCount = release.TrackCount,
                TrackPlayedCount = release.PlayedCount
            };
        }

        public ReleaseList ShallowCopy()
        {
            return (ReleaseList)MemberwiseClone();
        }
    }
}