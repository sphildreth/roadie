using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("chatMessage")]
    public class ChatMessage : EntityBase
    {
        [Column("message")]
        [Required]
        [MaxLength(5000)]
        public string Message { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")] [Required] public int UserId { get; set; }
    }
}