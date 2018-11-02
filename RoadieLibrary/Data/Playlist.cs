using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    [Table("playlist")]
    public partial class Playlist : NamedEntityBase
    {

        [Column("isPublic")]
        public bool IsPublic { get; set; }

        [Column("Description")]
        [MaxLength(1000)]
        public string Description { get; set; }

        [Column("userId")]
        public int? UserId { get; set; }

        [Column("thumbnail", TypeName = "blob")]
        public byte[] Thumbnail { get; set; }

        public ApplicationUser User { get; set; }

        public ICollection<PlaylistTrack> Tracks { get; set; }
    }
}
