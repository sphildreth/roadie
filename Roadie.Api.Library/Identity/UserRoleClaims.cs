using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    [Table("userRoleClaims")]
    public class UserRoleClaims : IdentityRoleClaim<int>
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int Id { get; set; }

        [Column("userRoleId")] 
        public override int RoleId { get; set; }

        [Required]
        [Column("claimType", TypeName = "varchar(200)")]
        public new string ClaimType { get; set; }

        [Required]
        [Column("claimValue", TypeName = "varchar(200)")]
        public new string ClaimValue { get; set; }

        [ForeignKey(nameof(RoleId))]
        [InverseProperty("RoleClaims")]
        public virtual UserRole UserRole { get; set; }
    }
}