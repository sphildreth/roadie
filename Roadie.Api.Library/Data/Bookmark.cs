using Roadie.Library.Enums;
using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("bookmark")]
    public partial class Bookmark : EntityBase
    {
        [Column("bookmarkTargetId")]
        public int BookmarkTargetId { get; set; }

        // public short? Type { get; set; }

        [Column("bookmarkType")]
        public BookmarkType? BookmarkType { get; set; }

        [Column("Comment")]
        [MaxLength(4000)]
        public string Comment { get; set; }

        [Column("position")]
        public int? Position { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("Bookmarks")]
        public virtual User User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }
    }
}