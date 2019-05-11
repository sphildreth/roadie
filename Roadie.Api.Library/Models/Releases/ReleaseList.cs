using Mapster;
using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    [DebuggerDisplay("DatabaseId [{ DatabaseId }] Name [{ ReleaseName }] IsValid [{ IsValid }]")]
    public class ReleaseList : EntityInfoModelBase
    {
        public DataToken Release { get; set; }
        public DataToken Artist { get; set; }
        public Image ArtistThumbnail { get; set; }
        public LibraryStatus? LibraryStatus { get; set; }
        public IEnumerable<ReleaseMediaList> Media { get; set; }
        public short? Rating { get; set; }
        public decimal? Rank { get; set; }

        [JsonIgnore]
        [AdaptIgnore]
        public string ReleaseName
        {
            get
            {
                return this.Release?.Text;
            }
        }

        public string ReleaseDate
        {
            get
            {
                return this.ReleaseDateDateTime.HasValue ? this.ReleaseDateDateTime.Value.ToUniversalTime().ToString("yyyy-MM-dd") : null;
            }
        }

        [JsonIgnore]
        public DateTime? ReleaseDateDateTime { get; set; }

        public string ReleasePlayUrl { get; set; }

        public string ReleaseYear
        {
            get
            {
                return this.ReleaseDateDateTime.HasValue ? this.ReleaseDateDateTime.Value.ToUniversalTime().ToString("yyyy") : null;
            }
        }

        public Image Thumbnail { get; set; }
        public int? TrackCount { get; set; }
        public int? TrackPlayedCount { get; set; }
        public UserRelease UserRating { get; set; }
        public Statuses? Status { get; set; }
        public DataToken Genre { get; set; }
        public DateTime? LastPlayed { get; set; }
        public decimal? Duration { get; set; }
        public string DurationTime
        {
            get
            {
                if(!this.Duration.HasValue)
                {
                    return "--:--";
                }
                return new TimeInfo(this.Duration.Value).ToFullFormattedString();
            }
        }
        public int? MediaCount { get; set; }

        public bool IsValid
        {
            get
            {
                var artistName = this.Artist?.Text;
                var releaseName = this.Release?.Text;
                return this.Id != Guid.Empty &&
                       !string.IsNullOrEmpty(artistName) &&
                       !string.IsNullOrEmpty(releaseName);
            }
        }

        /// <summary>
        /// This is populated on a request to get the list of releases for a collection.
        /// </summary>
        public int? ListNumber { get; set; }

        public ReleaseList ShallowCopy()
        {
            return (ReleaseList)this.MemberwiseClone();
        }

        public static ReleaseList FromDataRelease(Data.Release release, Data.Artist artist, string baseUrl, Image artistThumbnail, Image thumbnail)
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
                ReleasePlayUrl = $"{ baseUrl }/play/release/{ release.RoadieId}",
                Status = release.Status,
                Thumbnail = thumbnail,
                TrackCount = release.TrackCount,
                TrackPlayedCount = release.PlayedCount
            };
        }
    }
}
