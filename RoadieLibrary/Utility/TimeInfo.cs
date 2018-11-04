using Roadie.Library.Extensions;
using System;

namespace Roadie.Library.Utility
{
    public sealed class TimeInfo
    {
        public decimal Days { get; set; }

        public string DaysFormatted
        {
            get
            {
                if (this.Days > -1)
                {
                    return this.Days.ToString("000");
                }
                return "--";
            }
        }

        public decimal Hours { get; set; }

        public string HoursForamtted
        {
            get
            {
                if (this.Hours > -1)
                {
                    return this.Hours.ToString("00");
                }
                return "--";
            }
        }

        public decimal Minutes { get; set; }

        public string MinutesFormatted
        {
            get
            {
                if (this.Minutes > -1)
                {
                    return this.Minutes.ToString("00");
                }
                return "--";
            }
        }

        public decimal Seconds { get; set; }

        public string SecondsFormatted
        {
            get
            {
                if (this.Seconds > -1)
                {
                    return this.Seconds.ToString("00");
                }
                return "--";
            }
        }

        public decimal? Years { get; set; }

        public string YearsFormatted
        {
            get
            {
                if (this.Years.HasValue && this.Years.Value > 0)
                {
                    return this.Years.Value.ToString("0");
                }
                return null;
            }
        }

        public TimeInfo(decimal milliseconds)
        {
            var secondsTotal = milliseconds / 1000;
            var minutesTotal = Math.Floor(secondsTotal / 60);
            var hoursTotal = Math.Floor(minutesTotal / 60);
            var daysTotal = Math.Floor(hoursTotal / 24);
            var yearsTotal = Math.Floor(daysTotal / 365);

            this.Seconds = Math.Floor(secondsTotal - (minutesTotal * 60));
            this.Minutes = minutesTotal - (hoursTotal * 60);
            this.Hours = hoursTotal - (daysTotal * 24);
            this.Days = daysTotal - (yearsTotal * 365);
            this.Years = Math.Floor(daysTotal / 365);
        }

        public string ToFullFormattedString()
        {
            var yearsFormatted = this.YearsFormatted;
            return string.Format("{0}{1}", string.IsNullOrEmpty(yearsFormatted) ? string.Empty : yearsFormatted + ":", this.ToString("{DaysFormatted}:{HoursForamtted}:{MinutesFormatted}:{SecondsFormatted}"));
        }

        public string ToString(string format)
        {
            return format.FormatWith(this);
        }
    }
}