using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Identity
{
    [Table("usersInRoles")]
    public class UsersInRoles
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }
        [Column("userId")]
        public int UserId { get; set; }
        [Column("userRoleId")]
        public int UserRoleId { get; set; }
    }
}
