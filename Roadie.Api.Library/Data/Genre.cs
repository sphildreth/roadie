using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("genre")]
    public partial class Genre : EntityBase
    {
        public ICollection<ArtistGenre> Artists { get; set; }

        public ICollection<Comment> Comments { get; set; }

        [Column("name")]
        [MaxLength(100)] 
        [Required]
        public string Name { get; set; }

        [Column("sortName")]
        [MaxLength(100)]
        public string SortName { get; set; }

        public string SortNameValue => string.IsNullOrEmpty(SortName) ? Name : SortName;

        [Column("description")]
        [MaxLength(4000)]
        public string Description { get; set; }

        [Column("alternateNames", TypeName = "text")]
        [MaxLength(65535)]
        public string AlternateNames { get; set; }

        [Column("tags", TypeName = "text")]
        [MaxLength(65535)]
        public string Tags { get; set; }

        [Obsolete("Images moved to file system")]
        [Column("thumbnail", TypeName = "blob")]
        [MaxLength(65535)]
        public byte[] Thumbnail { get; set; }

        [Column("normalizedName")] [MaxLength(100)] public string NormalizedName { get; set; }

        public ICollection<ReleaseGenre> Releases { get; set; }

        public Genre()
        {
            Releases = new HashSet<ReleaseGenre>();
            Artists = new HashSet<ArtistGenre>();
            Comments = new HashSet<Comment>();
            Status = Enums.Statuses.Ok;
        }
    }
}