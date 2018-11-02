using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    [Table("playlisttrack")]
    public partial class PlaylistTrack : EntityBase
    {
        [Column("listNumber")]
        [Required]
        public int ListNumber { get; set; }
        [Column("trackId")]
        public int TrackId { get; set; }
        [Column("playListId")]
        public int PlayListId { get; set; }

        public Track Track { get; set; }
        public Playlist Playlist { get; set; }
    }
}
