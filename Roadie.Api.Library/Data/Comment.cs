using Roadie.Library.Enums;
using Roadie.Library.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("comment")]
    public partial class Comment : EntityBase
    {
        [Column("artistId")]
        public int? ArtistId { get; set; }

        [ForeignKey(nameof(ArtistId))]
        [InverseProperty("Comments")]
        public virtual Artist Artist { get; set; }

        [Column("comment")]
        [MaxLength(25500)]
        [Required]
        public string Cmt { get; set; }

        [Column("collectionId")]
        public int? CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        [InverseProperty("Comments")]
        public virtual Collection Collection { get; set; }

        [Column("genreId")]
        public int? GenreId { get; set; }

        [ForeignKey(nameof(GenreId))]
        [InverseProperty("Comments")]
        public virtual Genre Genre { get; set; }

        [NotMapped]
        public new bool? IsLocked { get; set; }

        [Column("labelId")]
        public int? LabelId { get; set; }

        [ForeignKey(nameof(LabelId))]
        [InverseProperty("Comments")]
        public virtual Label Label { get; set; }

        [Column("playlistId")]
        public int? PlaylistId { get; set; }

        [ForeignKey(nameof(PlaylistId))]
        [InverseProperty("Comments")]
        public virtual Playlist Playlist { get; set; }

        [InverseProperty("Comment")]
        public virtual ICollection<CommentReaction> Reactions { get; set; }

        [Column("releaseId")]
        public int? ReleaseId { get; set; }

        [ForeignKey(nameof(ReleaseId))]
        [InverseProperty("Comments")]
        public virtual Release Release { get; set; }

        [Column("replyToCommentId")]
        public int? ReplyToCommentId { get; set; }

        [Column("trackId")]
        public int? TrackId { get; set; }

        [ForeignKey(nameof(TrackId))]
        [InverseProperty("Comments")]
        public virtual Track Track { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("Comments")]
        public virtual User User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        public Comment()
        {
            Reactions = new HashSet<CommentReaction>();
            Status = Statuses.Ok;
        }
    }
}