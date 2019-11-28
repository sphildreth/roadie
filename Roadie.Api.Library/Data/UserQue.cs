using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("userQue")]
    public class UserQue : EntityBase
    {
        [Column("isCurrent")]
        public bool? IsCurrent { get; set; }

        [Column("position")]
        public long? Position { get; set; }

        [Column("queSortOrder")]
        [Required]
        public short QueSortOrder { get; set; }

        [ForeignKey(nameof(TrackId))]
        [InverseProperty("UserQues")]
        public virtual Track Track { get; set; }

        [Column("trackId")]
        [Required]
        public int TrackId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("UserQues")]
        public virtual User User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }
    }
}