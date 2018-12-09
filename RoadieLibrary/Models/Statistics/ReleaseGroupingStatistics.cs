using System;

namespace Roadie.Library.Models.Statistics
{
    /// <summary>
    /// A generic grouping of releases information for Playlists, Collections or Library
    /// </summary>
    [Serializable]
    public class ReleaseGroupingStatistics
    {
        public string FileSize { get; set; }
        public int? MissingTrackCount { get; set; }
        public int? ReleaseCount { get; set; }
        public int? ReleaseMediaCount { get; set; }
        public int? TrackCount { get; set; }
        public int? TrackPlayedCount { get; set; }
        public string TrackSize { get; set; }
    }
}