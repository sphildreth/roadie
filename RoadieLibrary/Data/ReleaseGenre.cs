using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("releaseGenreTable")]
    public partial class ReleaseGenre
    {
        public Genre Genre { get; set; }

        [Column("genreId")]
        [Required]
        public int? GenreId { get; set; }

        [Column("id")]
        [Key]
        public int Id { get; set; }

        public Release Release { get; set; }

        [Column("releaseId")]
        [Required]
        public int ReleaseId { get; set; }
    }
}