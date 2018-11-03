using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artistGenreTable")]
    public partial class ArtistGenre
    {
        public Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        public Genre Genre { get; set; }

        [Column("genreId")]
        [Required]
        public int? GenreId { get; set; }

        [Column("id")]
        [Key]
        public int Id { get; set; }
    }
}