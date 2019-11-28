using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("playlisttrack")]
    public class PlaylistTrack : EntityBase
    {
        [Column("listNumber")]
        [Required]
        public int ListNumber { get; set; }

        [ForeignKey(nameof(PlayListId))]
        [InverseProperty("Tracks")]
        public virtual Playlist Playlist { get; set; }

        [Column("playListId")]
        public int PlayListId { get; set; }

        public virtual Track Track { get; set; }

        [Column("trackId")]
        [InverseProperty("PlaylistTracks")]
        public int TrackId { get; set; }
    }
}