using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    /// <summary>
    /// Users In Roles
    /// </summary>
    [Table("usersInRoles")]
    public class UsersInRoles : IdentityUserRole<int>
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(RoleId))]
        [InverseProperty("UserRoles")]
        public virtual UserRole Role { get; set; }

        [Column("userRoleId")]
        [Required]
        public override int RoleId { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("UserRoles")]
        public virtual User User { get; set; }

        [Column("userId")]
        [Required]
        public override int UserId { get; set; }
    }
}