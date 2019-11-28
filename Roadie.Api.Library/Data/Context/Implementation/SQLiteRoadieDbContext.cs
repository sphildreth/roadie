using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context.Implementation
{
    /// <summary>
    /// SQLite implementation of DbContext
    /// </summary>
    public sealed class SQLiteRoadieDbContext : LinqDbContextBase
    {
        private static bool _created = false;

        public SQLiteRoadieDbContext(DbContextOptions options)
            : base(options)
        {
            if (!_created)
            {
                _created = true;
                Database.EnsureCreated();
            }
        }

        public override async Task<SortedDictionary<int, int>> RandomArtistIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            // TODO Rating?
            var sql = @"SELECT a.id
                        FROM artist a
                        WHERE(a.id NOT IN(select artistId FROM userartist where userId = {1} and isDisliked = 1))
                        OR(a.id IN (select artistId FROM userartist where userId = {1} and isFavorite = 1)
                        AND {2} = 1)
                        order by random()
                        LIMIT 0, {0}";
            var ids = await Artists.FromSqlRaw(sql, randomLimit, userId, doOnlyFavorites ? "1" : "0").Select(x => x.Id).ToListAsync();
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomGenreIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var sql = @"SELECT g.id
                        FROM genre g
                        order by random()
                        LIMIT 0, {0}";
            var ids = await Genres.FromSqlRaw(sql, randomLimit).Select(x => x.Id).ToListAsync();
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomLabelIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var sql = @"SELECT l.id
                        FROM label l
                        order by random()
                        LIMIT 0, {0}";
            var ids = await Labels.FromSqlRaw(sql, randomLimit).Select(x => x.Id).ToListAsync();
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomReleaseIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            // TODO Rating?
            var sql = @"SELECT r.id
                        FROM release r
                        WHERE (r.id NOT IN (select releaseId FROM userrelease where userId = {1} and isDisliked = 1))
                        OR (r.id IN (select releaseId FROM userrelease where userId = {1} and isFavorite = 1)
                            AND {2} = 1)
                        order by random()
                        LIMIT 0, {0}";
            var ids = await Releases.FromSqlRaw(sql, randomLimit, userId, doOnlyFavorites ? "1" : "0").Select(x => x.Id).ToListAsync();
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomTrackIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            // When using the regular 'FromSqlRaw' with parameters SQLite returns no records.

            var df = doOnlyFavorites ? "1" : "0";
            var dr = doOnlyRated ? "1" : "0";
            var sql = @$"SELECT t.id
                        FROM track t
                        WHERE ((t.rating > 0 AND {dr} = 1) OR {dr} = 0)
                        AND ((t.id NOT IN (select tt.id
                                            FROM track tt
                                            JOIN releasemedia rm on (tt.releaseMediaId = rm.id)
                                            JOIN userartist ua on (rm.id = ua.artistId)
                                            WHERE ua.userId = {userId} AND ua.isDisliked = 1))
                            AND (t.id NOT IN (select tt.id
                                            FROM track tt
                                            JOIN releasemedia rm on (tt.releaseMediaId = rm.id)
                                            JOIN userrelease ur on (rm.releaseId = ur.releaseId)
                                            WHERE ur.userId = {userId} AND ur.isDisliked = 1))
                            AND (t.id NOT IN (select tt.id
                                            FROM track tt
                                            JOIN usertrack ut on (tt.id = ut.trackId)
                                            WHERE ut.userId = {userId} AND ut.isDisliked = 1)))
                        AND ((t.id IN (select tt.id
                                        FROM track tt
                               JOIN usertrack ut on (tt.id = ut.trackId)
                                     WHERE ut.userId = {userId} AND ut.isFavorite = 1) AND {df} = 1) OR {df} = 0)
                        order by random()
                        LIMIT 0, {randomLimit}";
            var ids = await Tracks.FromSqlRaw(sql, randomLimit).Select(x => x.Id).ToListAsync();
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }
    }
}