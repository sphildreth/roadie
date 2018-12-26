using Roadie.Library.Utility;
using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class CollectionStatistics
    {
        public int? ArtistCount { get; set; }
        public string FileSize { get; set; }
        public int? MissingTrackCount { get; set; }
        public int? ReleaseCount { get; set; }
        public int? ReleaseMediaCount { get; set; }
        public string ReleaseTrackTime { get; set; }
        public int? TrackCount { get; set; }
        public int? TrackPlayedCount { get; set; }
        public string TrackTime { get; set; }
        public decimal? Duration { get; set; }

        public string DurationTime
        {
            get
            {
                if (!this.Duration.HasValue)
                {
                    return "--:--";
                }
                return new TimeInfo(this.Duration.Value).ToFullFormattedString();
            }

        }
    }
}