using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    [Table("usersInRoles")]
    public class ApplicationUserRole : IdentityUserRole<int>
    {
        public int Id { get; set; }
        public virtual ApplicationRole Role { get; set; }

        [Column("userRoleId")]
        public override int RoleId { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}