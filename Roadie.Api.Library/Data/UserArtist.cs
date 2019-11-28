using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("userartist")]
    public class UserArtist : EntityBase
    {
        [ForeignKey(nameof(ArtistId))]
        [InverseProperty("UserArtists")]
        public virtual Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        [Column("isDisliked")]
        public bool? IsDisliked { get; set; }

        [Column("isFavorite")]
        public bool? IsFavorite { get; set; }

        [Column("rating")]
        public short Rating { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("ArtistRatings")]
        public virtual User User { get; set; }

        [Column("userId")]
        [Required]
        public int UserId { get; set; }

        public UserArtist()
        {
            Rating = 0;
        }
    }
}