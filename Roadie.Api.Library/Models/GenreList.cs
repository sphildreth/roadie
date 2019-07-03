using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public class GenreList : EntityInfoModelBase
    {
        public int? ArtistCount { get; set; }
        public DataToken Genre { get; set; }
        public int? ReleaseCount { get; set; }
    }
}