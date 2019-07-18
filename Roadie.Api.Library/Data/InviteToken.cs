using Roadie.Library.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("inviteToken")]
    public class InviteToken : EntityBase
    {
        [Column("createdByUserId")]
        public int CreatedByUserId { get; set; }

        [Column("expiresDate")]
        public DateTime? ExpiresDate { get; set; }

        [NotMapped]
        public new bool IsLocked { get; set; }
    }
}
