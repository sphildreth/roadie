using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Statistics
{
    [Serializable]
    public class UserStatistics
    {
        public ArtistList MostPlayedArtist { get; set; }
        public Releases.ReleaseList MostPlayedRelease { get; set; }
        public TrackList MostPlayedTrack { get; set; }
        public int? RatedArtists { get; set; }
        public int? DislikedArtists { get; set; }
        public int? FavoritedArtists { get; set; }
        public int? RatedReleases { get; set; }
        public int? DislikedReleases { get; set; }
        public int? FavoritedReleases { get; set; }
        public int? RatedTracks { get; set; }
        public int? PlayedTracks { get; set; }
        public int? FavoritedTracks { get; set; }
        public int? DislikedTracks { get; set; }
    }
}
