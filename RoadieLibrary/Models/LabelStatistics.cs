using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public class LabelStatistics
    {
        public int? TotalArtists { get; set; }
        public int? TotalReleases { get; set; }
    }
}