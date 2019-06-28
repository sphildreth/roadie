using Roadie.Library.Enums;
using Roadie.Library.Identity;
using Roadie.Library.Utility;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("commentReaction")]
    public partial class CommentReaction : EntityBase
    {
        public Comment Comment { get; set; }

        public ApplicationUser User { get; set; }

        [Column("commentId")]
        [Required]
        public int CommentId { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        [NotMapped]
        public new bool? IsLocked { get; set; }

        [Column("reaction")]
        public string Reaction { get; set; }

        [NotMapped]
        public Enums.CommentReaction ReactionValue
        {
            get
            {
                return SafeParser.ToEnum<Enums.CommentReaction>(this.Reaction ?? "Unknown");
            }
        }
    }
}