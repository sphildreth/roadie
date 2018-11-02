using Roadie.Library.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("playlist")]
    public partial class Playlist : NamedEntityBase
    {
        [Column("Description")]
        [MaxLength(1000)]
        public string Description { get; set; }

        [Column("isPublic")]
        public bool IsPublic { get; set; }

        [Column("thumbnail", TypeName = "blob")]
        public byte[] Thumbnail { get; set; }

        public ICollection<PlaylistTrack> Tracks { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")]
        public int? UserId { get; set; }
    }
}