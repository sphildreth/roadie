using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class DateAndCount
    {
        public int? Count { get; set; }

        public string Date { get; set; }

        //public string Date
        //{
        //    get
        //    {
        //        return DateValue.ToString("yyyy-MM-dd");
        //    }
        //}

        //public DateTime DateValue { get; set; }
    }
}