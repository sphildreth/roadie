using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class ReleaseStatistics
    {
        public int? MediaCount { get; set; }
        public int? MissingTrackCount { get; set; }
        public int? TrackCount { get; set; }
        public int? TrackPlayedCount { get; set; }
        public string TrackSize { get; set; }
        public string TrackTime { get; set; }
    }
}