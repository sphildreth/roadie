using Newtonsoft.Json;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class TrackList : EntityInfoModelBase
    {
        public int? MediaNumber { get; set; }
        public int? TrackNumber { get; set; }
        public DataToken Track { get; set; }
        public DataToken Release { get; set; }
        public Image ReleaseThumbnail { get; set; }
        public DataToken Artist { get; set; }
        public Image ArtistThumbnail { get; set; }
        public DataToken TrackArtist { get; set; }
        public Image TrackArtistThumbnail { get; set; }
        public string Title { get; set; }
        public int? Duration { get; set; }
        public string DurationTime
        {
            get
            {
                return this.Duration.HasValue ? TimeSpan.FromSeconds(this.Duration.Value / 1000).ToString(@"hh\:mm\:ss") : "--:--";
            }
        }
        public string DurationTimeShort
        {
            get
            {
                return this.Duration.HasValue ? TimeSpan.FromSeconds(this.Duration.Value / 1000).ToString(@"mm\:ss") : "--:--";
            }
        }
        public short? ReleaseRating { get; set; }
        public short? Rating { get; set; }
        public UserTrack UserRating { get; set; }
        public int? PlayedCount { get; set; }
        public int? FavoriteCount { get; set; }
        public Image Thumbnail { get; set; }
        public string TrackPlayUrl { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string PartTitles { get; set; }

        public IEnumerable<string> PartTitlesList
        {
            get
            {
                if (string.IsNullOrEmpty(this.PartTitles))
                {
                    return null;
                }
                return this.PartTitles.Split('|');
            }
        }

        [JsonIgnore]
        public DateTime? ReleaseDate { get; set; }
        public int? Year
        {
            get
            {
                if(this.ReleaseDate.HasValue)
                {
                    return this.ReleaseDate.Value.Year;
                }
                return null; 
            }
        }
        public int? FileSize { get; set; }
    }
}
