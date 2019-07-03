using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    [Table("userRoleClaims")]
    public class ApplicationRoleClaim : IdentityRoleClaim<int>
    {
        public virtual ApplicationRole Role { get; set; }

        [Column("userRoleId")] public override int RoleId { get; set; }
    }
}