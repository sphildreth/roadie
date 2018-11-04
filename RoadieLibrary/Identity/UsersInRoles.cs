using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    [Table("usersInRoles")]
    public class UsersInRoles
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }

        public ApplicationRole Role { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")]
        public int UserId { get; set; }

        [Column("userRoleId")]
        public int UserRoleId { get; set; }
    }
}