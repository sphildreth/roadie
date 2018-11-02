using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    [Table("releaselabel")]
    public class ReleaseLabel : BeginAndEndEntityBase
    {
        [Column("releaseId")]
        public int ReleaseId { get; set; }

        public Release Release { get; set; }

        [Column("labelId")]
        public int LabelId { get; set; }

        public Label Label { get; set; }

        [Column("catalogNumber")]
        public string CatalogNumber { get; set; }

    }
}
