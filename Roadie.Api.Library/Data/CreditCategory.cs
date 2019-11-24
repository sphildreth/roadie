using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("creditCategory")]
    public partial class CreditCategory : NamedEntityBase
    {
        [Column("description")]
        [MaxLength(4000)]
        public string Description { get; set; }

        [MaxLength(100)]
        [Column("name")]
        public override string Name { get; set; }

        [NotMapped]
        public override string SortName { get; set; }
    }
}