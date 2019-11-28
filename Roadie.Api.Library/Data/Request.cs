using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("request")]
    public class Request : EntityBase
    {
        [Column("description")]
        [MaxLength(500)]
        [Required]
        public string Description { get; set; }

        [NotMapped]
        public new bool? IsLocked { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("Requests")]
        public virtual User User { get; set; }

        [Column("userId")]
        public int? UserId { get; set; }
    }
}