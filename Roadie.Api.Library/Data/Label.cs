using Roadie.Library.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("label")]
    public partial class Label : BeginAndEndNamedEntityBase
    {
        [Column("artistCount")] public int? ArtistCount { get; set; }

        public ICollection<Comment> Comments { get; set; }

        [Column("discogsId")] [MaxLength(50)] public string DiscogsId { get; set; }

        [Column("imageUrl")] [MaxLength(500)] public string ImageUrl { get; set; }

        [Column("musicBrainzId")]
        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [Column("profile", TypeName = "text")]
        [MaxLength(65535)]
        public string Profile { get; set; }

        [Column("releaseCount")] public int? ReleaseCount { get; set; }

        public ICollection<ReleaseLabel> ReleaseLabels { get; set; }

        [Column("trackCount")] public int? TrackCount { get; set; }

        public Label()
        {
            ReleaseLabels = new HashSet<ReleaseLabel>();
            Comments = new HashSet<Comment>();
            Status = Statuses.Ok;
        }
    }
}