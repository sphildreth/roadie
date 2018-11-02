using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    public abstract class BeginAndEndEntityBase : EntityBase
    {
        [Column("beginDate", TypeName = "date")]
        public DateTime? BeginDate { get; set; }

        [Column("endDate", TypeName = "date")]
        public DateTime? EndDate { get; set; }
    }
}
