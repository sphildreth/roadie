using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("submission")]
    public partial class Submission : EntityBase
    {
        public ApplicationUser User { get; set; }

        [Column("userId")]
        public int UserId { get; set; }
    }
}