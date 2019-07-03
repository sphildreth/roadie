using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("genre")]
    public partial class Genre : EntityBase
    {
        public ICollection<ArtistGenre> Artists { get; set; }

        public ICollection<Comment> Comments { get; set; }

        [Column("name")] [MaxLength(100)] public string Name { get; set; }

        public ICollection<ReleaseGenre> Releases { get; set; }

        public Genre()
        {
            Releases = new HashSet<ReleaseGenre>();
            Artists = new HashSet<ArtistGenre>();
            Comments = new HashSet<Comment>();
        }
    }
}