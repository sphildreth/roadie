using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class DateAndCount
    {
        public int? Count { get; set; }
        public string Date { get; set; }
    }
}