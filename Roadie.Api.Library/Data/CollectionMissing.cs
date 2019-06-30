using Roadie.Library.Enums;
using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("collectionMissing")]
    public partial class CollectionMissing
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("collectionId")]
        public int CollectionId { get; set; }
        [Column("isArtistFound")]
        public bool IsArtistFound { get; set; }
        [Column("position")]
        public int Position { get; set; }
        [Column("artist")]
        [MaxLength(1000)]
        public string Artist { get; set; }
        [Column("release")]
        [MaxLength(1000)]
        public string Release { get; set; }
    }
}
