using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    [Table("userClaims")]
    public class UserClaims : IdentityUserClaim<int>
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int Id { get; set; }

        [Required]
        [Column("userId")]
        public override int UserId { get; set; }

        [Required]
        [Column("claimType")]
        [StringLength(200)]
        public override string ClaimType { get; set; }

        [Required]
        [Column("claimValue")]
        [StringLength(200)]
        public override string ClaimValue { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("UserClaims")]
        public virtual User User { get; set; }
    }
}