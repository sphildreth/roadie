using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("userrelease")]
    public class UserRelease : EntityBase
    {
        [Column("isDisliked")]
        public bool? IsDisliked { get; set; }

        [Column("isFavorite")]
        public bool? IsFavorite { get; set; }

        [Column("rating")]
        public short Rating { get; set; }

        [ForeignKey(nameof(ReleaseId))]
        [InverseProperty("UserRelases")]
        public virtual Release Release { get; set; }

        [Column("releaseId")]
        [Required]
        public int ReleaseId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("UserReleases")]
        public virtual User User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        public UserRelease()
        {
            Rating = 0;
        }
    }
}