using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    [Table("userQue")]
    public class UserQue : EntityBase
    {
        public ApplicationUser User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        public Track Track { get; set; }

        [Column("trackId")]
        [Required]
        public int TrackId { get; set; }

        [Column("queSortOrder")]
        [Required]
        public short QueSortOrder { get; set; }

        [Column("position")]
        public long? Position { get; set; }

        [Column("isCurrent")]
        public bool? IsCurrent { get; set; }

    }
}
