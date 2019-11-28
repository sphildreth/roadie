using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artistGenreTable")]
    public class ArtistGenre
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        [Column("genreId")]
        [Required]
        public int GenreId { get; set; }

        [ForeignKey(nameof(ArtistId))]
        [InverseProperty("Genres")]
        public virtual Artist Artist { get; set; }

        [ForeignKey(nameof(GenreId))]
        [InverseProperty("Artists")]
        public virtual Genre Genre { get; set; }
    }
}