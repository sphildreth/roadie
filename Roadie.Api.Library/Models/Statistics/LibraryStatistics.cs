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
                if ((ArtistCount ?? 0) > 0) return ArtistCount.Value.ToString("000000");
                return "---";
            }
        }

        public string FormattedCollectionCount
        {
            get
            {
                if ((CollectionCount ?? 0) > 0) return CollectionCount.Value.ToString("000");
                return "--";
            }
        }

        public string FormattedLabelCount
        {
            get
            {
                if ((LabelCount ?? 0) > 0) return LabelCount.Value.ToString("00000");
                return "---";
            }
        }

        public string FormattedPlayedCount
        {
            get
            {
                if ((PlayedCount ?? 0) > 0) return PlayedCount.Value.ToString("000000");
                return "---";
            }
        }

        public string FormattedPlaylistCount
        {
            get
            {
                if ((PlaylistCount ?? 0) > 0) return PlaylistCount.Value.ToString("000");
                return "--";
            }
        }

        public string FormattedReleaseCount
        {
            get
            {
                if ((ReleaseCount ?? 0) > 0) return ReleaseCount.Value.ToString("000000");
                return "---";
            }
        }

        public string FormattedReleaseMediaCount
        {
            get
            {
                if ((ReleaseMediaCount ?? 0) > 0) return ReleaseMediaCount.Value.ToString("000000");
                return "---";
            }
        }

        public string FormattedTotalTrackDuration
        {
            get
            {
                if (TotalTrackDuration.HasValue)
                {
                    var ti = new TimeInfo(TotalTrackDuration.Value);
                    var t = ti.ToFullFormattedString();
                    return t;
                }

                return "--:--:--";
            }
        }

        public string FormattedTotalTrackSize
        {
            get
            {
                if (TotalTrackSize.HasValue) return TotalTrackSize.ToFileSize();
                return "--";
            }
        }

        public string FormattedTrackCount
        {
            get
            {
                if ((TrackCount ?? 0) > 0) return TrackCount.Value.ToString("0000000");
                return "---";
            }
        }

        public string FormattedUserCount
        {
            get
            {
                if ((UserCount ?? 0) > 0) return UserCount.Value.ToString("00");
                return "--";
            }
        }

        public int? LabelCount { get; set; }
        public DateTime? LastScan { get; set; }
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