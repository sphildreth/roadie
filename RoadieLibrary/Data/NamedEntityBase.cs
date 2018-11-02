using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    public abstract class NamedEntityBase : EntityBase
    {
        [Column("alternateNames", TypeName = "text")]
        [MaxLength(65535)]
        public string AlternateNames { get; set; }

        [MaxLength(250)]
        [Column("name")]
        public string Name { get; set; }

        [Column("sortName")]
        [MaxLength(250)]
        public string SortName { get; set; }

        [Column("tags", TypeName = "text")]
        [MaxLength(65535)]
        public string Tags { get; set; }

        [Column("urls", TypeName = "text")]
        [MaxLength(65535)]
        public string URLs { get; set; }
    }
}