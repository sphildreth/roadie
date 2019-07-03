using Roadie.Library.Models.Releases;
using System;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class UserStatistics
    {
        public int? DislikedArtists { get; set; }
        public int? DislikedReleases { get; set; }
        public int? DislikedTracks { get; set; }
        public int? FavoritedArtists { get; set; }
        public int? FavoritedReleases { get; set; }
        public int? FavoritedTracks { get; set; }
        public ArtistList MostPlayedArtist { get; set; }
        public ReleaseList MostPlayedRelease { get; set; }
        public TrackList MostPlayedTrack { get; set; }
        public int? PlayedTracks { get; set; }
        public int? RatedArtists { get; set; }
        public int? RatedReleases { get; set; }
        public int? RatedTracks { get; set; }
    }
}