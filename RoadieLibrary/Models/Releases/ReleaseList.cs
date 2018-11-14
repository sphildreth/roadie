using Mapster;
using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseList : EntityInfoModelBase
    {
        public DataToken Release { get; set; }
        public DataToken Artist { get; set; }
        public Image ArtistThumbnail { get; set; }
        public LibraryStatus? LibraryStatus { get; set; }
        public IEnumerable<ReleaseMediaList> Media { get; set; }
        public short? Rating { get; set; }

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
    }
}
