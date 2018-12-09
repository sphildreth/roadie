using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class DateAndCount
    {
        public int? Count { get; set; }
        public string Date { get; set; }
    }
}
