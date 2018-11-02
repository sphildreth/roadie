using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    public abstract class NamedEntityBase : EntityBase
    {
        [Column("sortName")]
        [MaxLength(250)]
        public string SortName { get; set; }

        [MaxLength(250)]
        [Column("name")]
        public string Name { get; set; }

        [Column("alternateNames", TypeName = "text")]
        [MaxLength(65535)]
        public string AlternateNames { get; set; }

        [Column("tags", TypeName = "text")]
        [MaxLength(65535)]
        public string Tags { get; set; }

        [Column("urls", TypeName = "text")]
        [MaxLength(65535)]
        public string URLs { get; set; }
    }
}
