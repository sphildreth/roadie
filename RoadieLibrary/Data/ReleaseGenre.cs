using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    [Table("releaseGenreTable")]
    public partial class ReleaseGenre
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }

        [Column("releaseId")]
        [Required]
        public int ReleaseId { get; set; }

        public Release Release { get; set; }

        [Column("genreId")]
        [Required]
        public int GenreId { get; set; }

        public Genre Genre { get; set; }
    }
}
