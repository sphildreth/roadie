using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("playlisttrack")]
    public partial class PlaylistTrack : EntityBase
    {
        [Column("listNumber")]
        [Required]
        public int ListNumber { get; set; }

        public Playlist Playlist { get; set; }

        [Column("playListId")]
        public int PlayListId { get; set; }

        public Track Track { get; set; }

        [Column("trackId")]
        public int TrackId { get; set; }
    }
}