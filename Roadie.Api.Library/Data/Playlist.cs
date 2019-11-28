using Roadie.Library.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("playlist")]
    public partial class Playlist : NamedEntityBase
    {
        [InverseProperty("Playlist")]
        public virtual ICollection<Comment> Comments { get; set; }

        [Column("Description")]
        [MaxLength(1000)]
        public string Description { get; set; }

        [Column("duration")]
        public int? Duration { get; set; }

        [Column("isPublic")]
        public bool IsPublic { get; set; }

        [Column("releaseCount")]
        public short ReleaseCount { get; set; }

        [NotMapped]
        public new string SortName { get; set; }

        [Column("trackCount")]
        public short TrackCount { get; set; }

        [InverseProperty("Playlist")]
        public virtual ICollection<PlaylistTrack> Tracks { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("Playlists")]
        public virtual User User { get; set; }

        [Column("userId")]
        public int? UserId { get; set; }

        public Playlist()
        {
            Comments = new HashSet<Comment>();
            Tracks = new HashSet<PlaylistTrack>();
        }
    }
}