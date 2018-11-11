using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("image")]
    public partial class Image : EntityBase
    {
        [Column("artistId")]
        public int? ArtistId { get; set; }

        [Column("image", TypeName = "mediumblob")]
        public byte[] Bytes { get; set; }

        [Column("caption")]
        [MaxLength(100)]
        public string Caption { get; set; }

        [Column("releaseId")]
        public int? ReleaseId { get; set; }

        [Column("signature")]
        [MaxLength(50)]
        public string Signature { get; set; }

        [Column("url")]
        [MaxLength(500)]
        public string Url { get; set; }

        public Artist Artist { get; set; }
        public Release Release { get; set; }
    }
}