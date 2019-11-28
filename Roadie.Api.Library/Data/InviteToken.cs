using Roadie.Library.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("inviteToken")]
    public class InviteToken : EntityBase
    {
        [ForeignKey(nameof(CreatedByUserId))]
        [InverseProperty(nameof(User.InviteTokens))]
        public virtual User CreatedByUser { get; set; }

        [Column("createdByUserId")]
        public int CreatedByUserId { get; set; }

        [Column("expiresDate")]
        public DateTime? ExpiresDate { get; set; }

        [NotMapped]
        public new bool IsLocked { get; set; }
    }
}