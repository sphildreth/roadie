using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("submission")]
    public class Submission : EntityBase
    {
        [ForeignKey(nameof(UserId))]
        [InverseProperty("Submissions")]
        public virtual User User { get; set; }

        [Column("userId")]
        public int UserId { get; set; }
    }
}