using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class CollectionStatistics
    {
        public string FileSize { get; set; }
        public int? MissingCount { get; set; }
        public int? ReleaseCount { get; set; }
        public string ReleaseTrackTime { get; set; }
        public int? TrackCount { get; set; }
    }
}