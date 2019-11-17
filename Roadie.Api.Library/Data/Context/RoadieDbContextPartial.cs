using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context
{
    public abstract partial class RoadieDbContext : DbContext, IRoadieDbContext
    {
        public abstract Task<SortedDictionary<int, int>> RandomArtistIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomGenreIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomLabelIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomReleaseIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomTrackIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<Artist> MostPlayedArtist(int userId);

        public abstract Task<Release> MostPlayedRelease(int userId);

        public abstract Task<Track> MostPlayedTrack(int userId);

        public abstract Task<Track> LastPlayedTrack(int userId);

        public abstract Task<Artist> LastPlayedArtist(int userId);

        public abstract Task<Release> LastPlayedRelease(int userId);
    }
}