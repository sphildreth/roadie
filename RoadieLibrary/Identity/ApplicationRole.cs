using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    [Table("userrole")]
    public partial class ApplicationRole : IdentityRole<int>
    {
        [Column("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [Column("description")]
        [StringLength(200)]
        public string Description { get; set; }

        //[Column("id")]
        //[Key]
        //public override int Id { get; set; }

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

        public virtual ICollection<ApplicationRoleClaim> RoleClaims { get; set; }

        [Column("status")]
        public short? Status { get; set; }

        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}