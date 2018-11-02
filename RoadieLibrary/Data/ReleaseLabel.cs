using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("releaselabel")]
    public class ReleaseLabel : BeginAndEndEntityBase
    {
        [Column("catalogNumber")]
        public string CatalogNumber { get; set; }

        public Label Label { get; set; }

        [Column("labelId")]
        public int LabelId { get; set; }

        public Release Release { get; set; }

        [Column("releaseId")]
        public int ReleaseId { get; set; }
    }
}