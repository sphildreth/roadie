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
        [Column("comment")]
        [MaxLength(25500)]
        [Required]
        public string Cmt { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        [Column("replyToCommentId")]
        public int? ReplyToCommentId { get; set; }

        [Column("artistId")]
        public int? ArtistId { get; set; }

        [Column("collectionId")]
        public int? CollectionId { get; set; }

        [Column("genreId")]
        public int? GenreId { get; set; }

        [Column("labelId")]
        public int? LabelId { get; set; }

        [Column("playlistId")]
        public int? PlaylistId { get; set; }

        [Column("releaseId")]
        public int? ReleaseId { get; set; }

        [Column("trackId")]
        public int? TrackId { get; set; }

        [NotMapped]
        public new bool? IsLocked { get; set; }

        public ICollection<CommentReaction> Reactions { get; set; }

        public Comment()
        {
            this.Reactions = new HashSet<CommentReaction>();
            this.Status = Statuses.Ok;
        }
    }
}