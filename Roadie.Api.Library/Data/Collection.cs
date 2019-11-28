using Roadie.Library.Enums;
using Roadie.Library.Identity;
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

        [Column("collectionType")]
        public CollectionType? CollectionType { get; set; }

        [InverseProperty("Collection")]
        public virtual ICollection<Comment> Comments { get; set; }

        [Column("description")]
        [MaxLength(4000)]
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

        [ForeignKey(nameof(MaintainerId))]
        [InverseProperty(nameof(User.Collections))]
        public virtual User Maintainer { get; set; }

        [InverseProperty("Collection")]
        public virtual ICollection<CollectionRelease> Releases { get; set; }

        [InverseProperty("Collection")]
        public virtual ICollection<CollectionMissing> MissingReleases { get; set; }
    }
}