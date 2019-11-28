using Roadie.Library.Identity;
using Roadie.Library.Utility;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("commentReaction")]
    public class CommentReaction : EntityBase
    {
        public Comment Comment { get; set; }

        [Column("commentId")]
        [Required]
        public int CommentId { get; set; }

        [NotMapped]
        public new bool? IsLocked { get; set; }

        [Column("reaction")]
        public string Reaction { get; set; }

        [NotMapped]
        public Enums.CommentReaction ReactionValue => SafeParser.ToEnum<Enums.CommentReaction>(Reaction ?? "Unknown");

        [ForeignKey(nameof(UserId))]
        [InverseProperty("CommentReactions")]
        public virtual User User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }
    }
}