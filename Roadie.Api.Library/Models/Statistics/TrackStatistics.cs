using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class TrackStatistics
    {
        public string FileSizeFormatted { get; set; }
        public string Time { get; set; }
        public int? DislikedCount { get; set; }
        public int? FavoriteCount { get; set; }
        public int? PlayedCount { get; set; }
    }
}