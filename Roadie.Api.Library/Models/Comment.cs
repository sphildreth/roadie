using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Library.Models
{
    public class Comment : EntityInfoModelBase
    {
        public Users.UserList User { get; set; }

        public Comment ReplyToComment { get; set; }

        [MaxLength(2500)]
        [Required]
        public string Cmt { get; set; }

        public bool IsDisliked { get; set; }

        public bool IsLiked { get; set; }

        public int? LikedCount { get; set; }
        public int? DislikedCount { get; set; }
    }
}
