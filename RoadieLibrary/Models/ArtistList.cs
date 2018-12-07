using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class ArtistList : EntityInfoModelBase
    {
        public DataToken Artist { get; set; }
        public short? Rating { get; set; }
        public UserArtist UserRating { get; set; }
        public int? ReleaseCount { get; set; }
        public int? TrackCount { get; set; }
        public int? PlayedCount { get; set; }
        public Image Thumbnail { get; set; }
        public DateTime? LastPlayed { get; set; }

        public static ArtistList FromDataArtist(Data.Artist artist, Image thumbnail)
        {
            return new ArtistList
            {
                DatabaseId = artist.Id,
                Id = artist.RoadieId,
                Artist = new DataToken
                {
                    Text = artist.Name,
                    Value = artist.RoadieId.ToString()
                },
                Thumbnail = thumbnail, // this.MakeArtistThumbnailImage(a.RoadieId),
                Rating = artist.Rating,
                CreatedDate = artist.CreatedDate,
                LastUpdated = artist.LastUpdated,
                LastPlayed = artist.LastPlayed,
                PlayedCount = artist.PlayedCount,
                ReleaseCount = artist.ReleaseCount,
                TrackCount = artist.TrackCount,
                SortName = artist.SortName
            };
        }
    }
}
