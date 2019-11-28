using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artistSimilar")]
    public class ArtistSimilar
    {
        [ForeignKey(nameof(ArtistId))]
        [InverseProperty("SimilarArtists")]
        public virtual Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(SimilarArtistId))]
        public virtual Artist SimilarArtist { get; set; }

        [Column("similarArtistId")]
        public int SimilarArtistId { get; set; }
    }
}