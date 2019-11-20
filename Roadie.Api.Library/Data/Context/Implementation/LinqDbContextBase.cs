using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context.Implementation
{
    /// <summary>
    /// Base DbContext using just LINQ statements against EF
    /// </summary>
    public abstract class LinqDbContextBase : RoadieDbContext
    {
        public LinqDbContextBase(DbContextOptions options)
            : base(options)
        {
        }

        public override async Task<Artist> LastPlayedArtist(int userId)
        {
            return await (from ut in UserTracks
                          join t in Tracks on ut.TrackId equals t.Id
                          join rm in ReleaseMedias on t.ReleaseMediaId equals rm.Id
                          join r in Releases on rm.ReleaseId equals r.Id
                          join a in Artists on r.ArtistId equals a.Id
                          where ut.UserId == userId
                          orderby ut.LastPlayed descending
                          select a).FirstOrDefaultAsync();
        }

        public override async Task<Release> LastPlayedRelease(int userId)
        {
            return await (from ut in UserTracks
                          join t in Tracks on ut.TrackId equals t.Id
                          join rm in ReleaseMedias on t.ReleaseMediaId equals rm.Id
                          join r in Releases on rm.ReleaseId equals r.Id
                          where ut.UserId == userId
                          orderby ut.LastPlayed descending
                          select r).FirstOrDefaultAsync();
        }

        public override async Task<Track> LastPlayedTrack(int userId)
        {
            return await (from ut in UserTracks
                          join t in Tracks on ut.TrackId equals t.Id
                          where ut.UserId == userId
                          orderby ut.LastPlayed descending
                          select t).FirstOrDefaultAsync();
        }

        public override async Task<Artist> MostPlayedArtist(int userId)
        {
            var mostPlayedTrack = await MostPlayedTrack(userId);
            if (mostPlayedTrack != null)
            {
                return await (from t in Tracks
                              join rm in ReleaseMedias on t.ReleaseMediaId equals rm.Id
                              join r in Releases on rm.ReleaseId equals r.Id
                              join a in Artists on r.ArtistId equals a.Id
                              where t.Id == mostPlayedTrack.Id
                              select a).FirstOrDefaultAsync();
            }
            return null;
        }

        public override async Task<Release> MostPlayedRelease(int userId)
        {
            var mostPlayedTrack = await MostPlayedTrack(userId);
            if (mostPlayedTrack != null)
            {
                return await (from t in Tracks
                              join rm in ReleaseMedias on t.ReleaseMediaId equals rm.Id
                              join r in Releases on rm.ReleaseId equals r.Id
                              where t.Id == mostPlayedTrack.Id
                              select r).FirstOrDefaultAsync();
            }
            return null;
        }

        public override async Task<Track> MostPlayedTrack(int userId)
        {
            return await (from ut in UserTracks
                          join t in Tracks on ut.TrackId equals t.Id
                          where ut.UserId == userId
                          orderby ut.PlayedCount descending
                          select t).FirstOrDefaultAsync();
        }

        public override async Task<SortedDictionary<int, int>> RandomArtistIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            List<Artist> randomArtists = null;
            if (doOnlyFavorites)
            {
                randomArtists = await (from ua in UserArtists
                                       join a in Artists on ua.ArtistId equals a.Id
                                       where ua.UserId == userId
                                       where ua.IsFavorite == true
                                       select a
                                       ).OrderBy(x => Guid.NewGuid())
                                        .Take(randomLimit)
                                        .ToListAsync();
            }
            else if (doOnlyRated)
            {
                randomArtists = await (from ua in UserArtists
                                       join a in Artists on ua.ArtistId equals a.Id
                                       where ua.UserId == userId
                                       where ua.Rating > 0
                                       select a
                                       ).OrderBy(x => Guid.NewGuid())
                                        .Take(randomLimit)
                                        .ToListAsync();
            }
            else
            {
                randomArtists = await Artists.OrderBy(x => Guid.NewGuid())
                                             .Take(randomLimit)
                                             .ToListAsync();
            }
            var dict = randomArtists.Select((x, i) => new { key = i, value = x.Id }).Take(randomLimit).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomGenreIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var randomGenres = await Genres.OrderBy(x => Guid.NewGuid())
                                           .Take(randomLimit)
                                           .ToListAsync();
            var dict = randomGenres.Select((x, i) => new { key = i, value = x.Id }).Take(randomLimit).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomLabelIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var randomLabels = await Labels.OrderBy(x => Guid.NewGuid()).Take(randomLimit).ToListAsync();
            var dict = randomLabels.Select((x, i) => new { key = i, value = x.Id }).Take(randomLimit).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomReleaseIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            List<Release> randomReleases = null;
            if (doOnlyFavorites)
            {
                randomReleases = await (from ur in UserReleases
                                        join r in Releases on ur.ReleaseId equals r.Id
                                        where ur.UserId == userId
                                        where ur.IsFavorite == true
                                        select r
                                       ).OrderBy(x => Guid.NewGuid())
                                        .Take(randomLimit)
                                        .ToListAsync();
            }
            else if (doOnlyRated)
            {
                randomReleases = await (from ur in UserReleases
                                        join r in Releases on ur.ReleaseId equals r.Id
                                        where ur.UserId == userId
                                        where ur.Rating > 0
                                        select r
                                       ).OrderBy(x => Guid.NewGuid())
                                        .Take(randomLimit)
                                        .ToListAsync();
            }
            else
            {
                randomReleases = await Releases.OrderBy(x => Guid.NewGuid())
                                               .Take(randomLimit)
                                               .ToListAsync();
            }
            var dict = randomReleases.Select((x, i) => new { key = i, value = x.Id }).Take(randomLimit).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomTrackIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            List<Track> randomTracks = null;
            if (doOnlyFavorites)
            {
                randomTracks = await (from ut in UserTracks
                                      join t in Tracks on ut.TrackId equals t.Id
                                      where ut.UserId == userId
                                      where ut.IsFavorite == true
                                      select t
                                       ).OrderBy(x => Guid.NewGuid())
                                        .Take(randomLimit)
                                        .ToListAsync();
            }
            else if (doOnlyRated)
            {
                randomTracks = await (from ut in UserTracks
                                      join t in Tracks on ut.TrackId equals t.Id
                                      where ut.UserId == userId
                                      where ut.Rating > 0
                                      select t
                                       ).OrderBy(x => Guid.NewGuid())
                                        .Take(randomLimit)
                                        .ToListAsync();
            }
            else
            {
                randomTracks = await Tracks.OrderBy(x => Guid.NewGuid())
                                           .Take(randomLimit)
                                           .ToListAsync();
            }
            var dict = randomTracks.Select((x, i) => new { key = i, value = x.Id }).Take(randomLimit).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }
    }
}