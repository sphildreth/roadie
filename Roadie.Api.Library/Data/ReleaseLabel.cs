using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("releaselabel")]
    public class ReleaseLabel : BeginAndEndEntityBase
    {
        [Column("catalogNumber")]
        public string CatalogNumber { get; set; }

        [ForeignKey(nameof(LabelId))]
        [InverseProperty("ReleaseLabels")]
        public virtual Label Label { get; set; }

        [Column("labelId")]
        public int LabelId { get; set; }

        [ForeignKey(nameof(ReleaseId))]
        [InverseProperty("Labels")]
        public virtual Release Release { get; set; }

        [Column("releaseId")]
        public int ReleaseId { get; set; }
    }
}