using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("collectionMissing")]
    public class CollectionMissing
    {
        [Column("artist")]
        [MaxLength(1000)]
        public string Artist { get; set; }

        [Column("collectionId")]
        public int CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        [InverseProperty("MissingReleases")]
        public virtual Collection Collection { get; set; }

        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("isArtistFound")]
        public bool IsArtistFound { get; set; }

        [Column("position")]
        public int Position { get; set; }

        [Column("release")]
        [MaxLength(1000)]
        public string Release { get; set; }
    }
}