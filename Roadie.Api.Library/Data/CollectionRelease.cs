using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("collectionrelease")]
    public class CollectionRelease : EntityBase
    {
        public Collection Collection { get; set; }

        [Column("collectionId")] [Required] public int CollectionId { get; set; }

        [Column("listNumber")] public int ListNumber { get; set; }

        public Release Release { get; set; }

        [Column("releaseId")] [Required] public int ReleaseId { get; set; }
    }
}