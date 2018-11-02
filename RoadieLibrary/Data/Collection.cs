using Roadie.Library.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("collection")]
    public partial class Collection : NamedEntityBase
    {
        [Column("collectionCount")]
        public int CollectionCount { get; set; }

        [Column("description")]
        [MaxLength(1000)]
        public string Description { get; set; }

        [Column("edition")]
        [MaxLength(200)]
        public string Edition { get; set; }

        [Column("listInCSV", TypeName = "text")]
        [MaxLength(65535)]
        public string ListInCSV { get; set; }

        [Column("listInCSVFormat")]
        [MaxLength(200)]
        public string ListInCSVFormat { get; set; }

        [Column("maintainerId")]
        public int MaintainerId { get; set; }

        [Column("thumbnail", TypeName = "blob")]
        public byte[] Thumbnail { get; set; }

        [Column("collectionType")]
        public CollectionType? CollectionType { get; set; }

        public ICollection<CollectionRelease> Releases { get; set; }
    }
}