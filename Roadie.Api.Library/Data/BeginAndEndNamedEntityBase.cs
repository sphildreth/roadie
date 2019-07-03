using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    public abstract class BeginAndEndNamedEntityBase : NamedEntityBase
    {
        [Column("beginDate")] public DateTime? BeginDate { get; set; }

        [Column("endDate")] public DateTime? EndDate { get; set; }
    }
}