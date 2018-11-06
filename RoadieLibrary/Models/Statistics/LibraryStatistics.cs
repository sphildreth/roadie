using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class LibraryStats
    {
        public int? ArtistCount { get; set; }
        public int? CollectionCount { get; set; }

        public string FormattedArtistCount
        {
            get
            {
                if ((this.ArtistCount ?? 0) > 0)
                {
                    return this.ArtistCount.Value.ToString("000000");
                }
                return "---";
            }
        }

        public string FormattedCollectionCount
        {
            get
            {
                if ((this.CollectionCount ?? 0) > 0)
                {
                    return this.CollectionCount.Value.ToString("000");
                }
                return "--";
            }
        }

        public string FormattedLabelCount
        {
            get
            {
                if ((this.LabelCount ?? 0) > 0)
                {
                    return this.LabelCount.Value.ToString("00000");
                }
                return "---";
            }
        }

        public string FormattedPlayedCount
        {
            get
            {
                if ((this.PlayedCount ?? 0) > 0)
                {
                    return this.PlayedCount.Value.ToString("000000");
                }
                return "---";
            }
        }

        public string FormattedPlaylistCount
        {
            get
            {
                if ((this.PlaylistCount ?? 0) > 0)
                {
                    return this.PlaylistCount.Value.ToString("000");
                }
                return "--";
            }
        }

        public string FormattedReleaseCount
        {
            get
            {
                if ((this.ReleaseCount ?? 0) > 0)
                {
                    return this.ReleaseCount.Value.ToString("000000");
                }
                return "---";
            }
        }

        public string FormattedReleaseMediaCount
        {
            get
            {
                if ((this.ReleaseMediaCount ?? 0) > 0)
                {
                    return this.ReleaseMediaCount.Value.ToString("000000");
                }
                return "---";
            }
        }

        public string FormattedTotalTrackDuration
        {
            get
            {
                if (this.TotalTrackDuration.HasValue)
                {
                    var ti = new TimeInfo(this.TotalTrackDuration.Value);
                    return ti.ToFullFormattedString();
                }
                return "--:--:--";
            }
        }

        public string FormattedTotalTrackSize
        {
            get
            {
                if (this.TotalTrackSize.HasValue)
                {
                    return this.TotalTrackSize.ToFileSize();
                }
                return "--";
            }
        }

        public string FormattedTrackCount
        {
            get
            {
                if ((this.TrackCount ?? 0) > 0)
                {
                    return this.TrackCount.Value.ToString("0000000");
                }
                return "---";
            }
        }

        public string FormattedUserCount
        {
            get
            {
                if ((this.UserCount ?? 0) > 0)
                {
                    return this.UserCount.Value.ToString("00");
                }
                return "--";
            }
        }

        public int? LabelCount { get; set; }
        public int? PlayedCount { get; set; }
        public int? PlaylistCount { get; set; }
        public int? ReleaseCount { get; set; }
        public int? ReleaseMediaCount { get; set; }
        public decimal? TotalTrackDuration { get; set; }
        public long? TotalTrackSize { get; set; }
        public int? TrackCount { get; set; }
        public int? UserCount { get; set; }
    }
}