using Roadie.Library.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    public class Comment : EntityInfoModelBase
    {
        [MaxLength(2500)] [Required] public string Cmt { get; set; }
        public int? DislikedCount { get; set; }
        public bool IsDisliked { get; set; }
        public bool IsLiked { get; set; }
        public int? LikedCount { get; set; }
        public Comment ReplyToComment { get; set; }
        public UserList User { get; set; }
    }
}