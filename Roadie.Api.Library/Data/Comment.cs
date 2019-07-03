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
        [Column("artistId")] public int? ArtistId { get; set; }

        [Column("comment")]
        [MaxLength(25500)]
        [Required]
        public string Cmt { get; set; }

        [Column("collectionId")] public int? CollectionId { get; set; }

        [Column("genreId")] public int? GenreId { get; set; }

        [NotMapped] public new bool? IsLocked { get; set; }

        [Column("labelId")] public int? LabelId { get; set; }

        [Column("playlistId")] public int? PlaylistId { get; set; }

        public ICollection<CommentReaction> Reactions { get; set; }

        [Column("releaseId")] public int? ReleaseId { get; set; }

        [Column("replyToCommentId")] public int? ReplyToCommentId { get; set; }

        [Column("trackId")] public int? TrackId { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")] [Required] public int UserId { get; set; }

        public Comment()
        {
            Reactions = new HashSet<CommentReaction>();
            Status = Statuses.Ok;
        }
    }
}