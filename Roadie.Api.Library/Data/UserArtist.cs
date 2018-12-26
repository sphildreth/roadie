using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("userartist")]
    public partial class UserArtist : EntityBase
    {
        public Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        [Column("isDisliked")]
        public bool? IsDisliked { get; set; }

        [Column("isFavorite")]
        public bool? IsFavorite { get; set; }

        [Column("rating")]
        public short Rating { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        public UserArtist()
        {
            this.Rating = 0;
        }
    }
}