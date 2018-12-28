using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class GenreList : EntityInfoModelBase
    {
        public DataToken Genre { get; set; }
        public int? ReleaseCount { get; set; }
        public int? ArtistCount { get; set; }

    }
}
