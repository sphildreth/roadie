using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("releasemedia")]
    public class ReleaseMedia : EntityBase
    {
        [Column("releaseMediaNumber")]
        public short MediaNumber { get; set; }

        [ForeignKey(nameof(ReleaseId))]
        [InverseProperty("Medias")]
        public virtual Release Release { get; set; }

        [Column("releaseId")]
        public int ReleaseId { get; set; }

        [Column("releaseSubTitle")]
        [MaxLength(500)]
        public string SubTitle { get; set; }

        [Column("trackCount")]
        [Required]
        public short TrackCount { get; set; }

        [InverseProperty("ReleaseMedia")]
        public virtual ICollection<Track> Tracks { get; set; }

        public ReleaseMedia()
        {
            Tracks = new HashSet<Track>();
        }
    }
}