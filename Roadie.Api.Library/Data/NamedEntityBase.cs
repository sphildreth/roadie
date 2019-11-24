using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    public abstract class NamedEntityBase : EntityBase
    {
        [Column("alternateNames", TypeName = "text")]
        [MaxLength(65535)]
        public virtual string AlternateNames { get; set; }

        [MaxLength(250)]
        [Column("name")]
        public virtual string Name { get; set; }

        [Column("sortName")] 
        [MaxLength(250)] 
        public virtual string SortName { get; set; }

        public virtual string SortNameValue => string.IsNullOrEmpty(SortName) ? Name : SortName;

        [Column("tags", TypeName = "text")]
        [MaxLength(65535)]
        public virtual string Tags { get; set; }

        [Column("urls", TypeName = "text")]
        [MaxLength(65535)]
        public virtual string URLs { get; set; }
    }
}