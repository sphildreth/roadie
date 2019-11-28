using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("releaseGenreTable")]
    public class ReleaseGenre
    {
        [ForeignKey(nameof(ReleaseId))]
        [InverseProperty("Genres")]
        public virtual Release Release { get; set; }

        [Column("releaseId")]
        [Required]
        public int ReleaseId { get; set; }

        [ForeignKey(nameof(GenreId))]
        [InverseProperty("Releases")]
        public virtual Genre Genre { get; set; }

        [Column("genreId")]
        [Required]
        public int GenreId { get; set; }

        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    }
}