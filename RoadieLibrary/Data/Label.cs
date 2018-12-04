using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("label")]
    public partial class Label : BeginAndEndNamedEntityBase
    {
        [Column("discogsId")]
        [MaxLength(50)]
        public string DiscogsId { get; set; }

        [Column("imageUrl")]
        [MaxLength(500)]
        public string ImageUrl { get; set; }

        [Column("musicBrainzId")]
        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [Column("profile", TypeName = "text")]
        [MaxLength(65535)]
        public string Profile { get; set; }

        [Column("artistCount")]
        public int? ArtistCount { get; set; } // TODO update this on artist folder scane


        [Column("releaseCount")]
        public int? ReleaseCount { get; set; } // TODO update this on artist folder scan

        [Column("trackCount")]
        public int? TrackCount { get; set; } // TODO update this on artist folder scan


        public List<ReleaseLabel> ReleaseLabels { get; set; }
    }
}