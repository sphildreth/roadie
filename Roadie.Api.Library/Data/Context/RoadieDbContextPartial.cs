﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context
{
    public abstract partial class RoadieDbContext : DbContext, IRoadieDbContext
    {
        public abstract Task<SortedDictionary<int, int>> RandomArtistIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomGenreIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomLabelIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomReleaseIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<SortedDictionary<int, int>> RandomTrackIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        public abstract Task<Artist> MostPlayedArtist(int userId);

        public abstract Task<Release> MostPlayedRelease(int userId);

        public abstract Task<Track> MostPlayedTrack(int userId);

        public abstract Task<Track> LastPlayedTrack(int userId);

        public abstract Task<Artist> LastPlayedArtist(int userId);

        public abstract Task<Release> LastPlayedRelease(int userId);
    }
}