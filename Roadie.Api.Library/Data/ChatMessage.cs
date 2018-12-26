using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    [Table("chatMessage")]
    public class ChatMessage : EntityBase
    {
        public ApplicationUser User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        [Column("message")]
        [Required]
        [MaxLength(5000)]
        public string Message { get; set; }
    }
}
