using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public sealed class GenreList : EntityInfoModelBase
    {
        public int? ArtistCount { get; set; }
        public DataToken Genre { get; set; }
        public int? ReleaseCount { get; set; }
        public Image Thumbnail { get; set; }

        public static GenreList FromDataGenre(Data.Genre genre, Image genreThumbnail, int? artistCount, int? releaseCount)
        {
            return new GenreList
            {
                Id = genre.RoadieId,
                Genre = new DataToken
                {
                    Text = genre.Name,
                    Value = genre.RoadieId.ToString()
                },
                SortName = genre.SortName,
                CreatedDate = genre.CreatedDate,
                LastUpdated = genre.LastUpdated,
                ArtistCount = artistCount,
                ReleaseCount = releaseCount,
                Thumbnail = genreThumbnail
            };
        }
    }
}