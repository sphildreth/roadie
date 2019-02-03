using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    [DebuggerDisplay("Artist Id {Id}, Name {Artist.Text}")]
    public class ArtistList : EntityInfoModelBase
    {
        public DataToken Artist { get; set; }
        public short? Rating { get; set; }
        public decimal? Rank { get; set; }
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
                Thumbnail = thumbnail, 
                Rating = artist.Rating,
                Rank = artist.Rank,
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

    public class ArtistListComparer : IEqualityComparer<ArtistList>
    {
        public bool Equals(ArtistList x, ArtistList y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if ((x != null && y == null) || (x == null && y != null))
            {
                return false;
            }
            return x.DatabaseId.Equals(y.DatabaseId) && x.Id.Equals(y.Id);
        }

        public int GetHashCode(ArtistList item)
        {
            return item.Id.GetHashCode();
        }
    }
}
