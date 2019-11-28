using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    /// <summary>
    /// Definition of a User Role
    /// </summary>
    [Table("userrole")]
    public class UserRole : IdentityRole<int>
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int Id { get; set; }

        [Column("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [Column("description")]
        [StringLength(200)]
        public string Description { get; set; }

        [Column("isLocked")]
        public bool? IsLocked { get; set; }

        [Column("lastUpdated")]
        public DateTime? LastUpdated { get; set; }

        [Column("name")]
        [Required]
        [StringLength(80)]
        public override string Name { get; set; }

        [Column("RoadieId")]
        [StringLength(36)]
        public string RoadieId { get; set; }

        [InverseProperty("UserRole")]
        public virtual ICollection<UserRoleClaims> RoleClaims { get; set; }

        [Column("status")]
        public short? Status { get; set; }

        [InverseProperty("Role")]
        public virtual ICollection<UsersInRoles> UserRoles { get; set; }
    }
}