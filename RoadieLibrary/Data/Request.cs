using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("request")]
    public partial class Request : EntityBase
    {
        [Column("description")]
        [MaxLength(500)]
        [Required]
        public string Description { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }
    }
}