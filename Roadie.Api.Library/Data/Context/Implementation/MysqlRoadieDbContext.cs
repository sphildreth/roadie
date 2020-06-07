using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context.Implementation
{
    /// <summary>
    /// MySQL/MariaDB implementation of DbContext using SQL statements for better performance.
    /// </summary>
    public sealed class MySQLRoadieDbContext : RoadieDbContext
    {
        public MySQLRoadieDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public override async Task<Artist> LastPlayedArtist(int userId)
        {
            var sql = @"SELECT a.*
                        FROM `usertrack` ut
                        join `track` t on (ut.trackId = t.id)
                        join `releasemedia` rm on (t.releaseMediaId = rm.id)
                        join `release` r on (rm.releaseId = r.id)
                        join `artist` a on (r.artistId = a.id)
                        where ut.userId = {0}
                        ORDER by ut.lastPlayed desc
                        LIMIT 1";
            return await Artists.FromSqlRaw(sql, userId).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public override async Task<Release> LastPlayedRelease(int userId)
        {
            var sql = @"SELECT r.*
                        FROM `usertrack` ut
                        join `track` t on (ut.trackId = t.id)
                        join `releasemedia` rm on (t.releaseMediaId = rm.id)
                        join `release` r on (rm.releaseId = r.id)
                        WHERE ut.userId = {0}
                        ORDER by ut.lastPlayed desc
                        LIMIT 1";
            return await Releases.FromSqlRaw(sql, userId)
                                 .Include(x => x.Artist)
                                 .FirstOrDefaultAsync()
                                 .ConfigureAwait(false);
        }

        public override async Task<Track> LastPlayedTrack(int userId)
        {
            var sql = @"SELECT t.*
                        FROM `usertrack` ut
                        join `track` t on (ut.trackId = t.id)
                        WHERE ut.userId = {0}
                        ORDER by ut.lastPlayed desc
                        LIMIT 1";
            return await Tracks.FromSqlRaw(sql, userId)
                               .Include(x => x.TrackArtist)
                               .Include(x => x.ReleaseMedia)
                               .Include("ReleaseMedia.Release")
                               .Include("ReleaseMedia.Release.Artist")
                               .FirstOrDefaultAsync()
                               .ConfigureAwait(false);
        }

        public override async Task<Artist> MostPlayedArtist(int userId)
        {
            var sql = @"SELECT a.*
                        FROM `usertrack` ut
                        join `track` t on (ut.trackId = t.id)
                        join `releasemedia` rm on (t.releaseMediaId = rm.id)
                        join `release` r on (rm.releaseId = r.id)
                        join `artist` a on (r.artistId = a.id)
                        where ut.userId = {0}
                        group by r.id
                        order by SUM(ut.playedCount) desc
                        LIMIT 1";
            return await Artists.FromSqlRaw(sql, userId).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public override async Task<Release> MostPlayedRelease(int userId)
        {
            var sql = @"SELECT r.*
                        FROM `usertrack` ut
                        join `track` t on (ut.trackId = t.id)
                        join `releasemedia` rm on (t.releaseMediaId = rm.id)
                        join `release` r on (rm.releaseId = r.id)
                        WHERE ut.userId = {0}
                        GROUP by r.id
                        ORDER by SUM(ut.playedCount) desc
                        LIMIT 1";
            return await Releases.FromSqlRaw(sql, userId)
                                 .Include(x => x.Artist)
                                 .FirstOrDefaultAsync()
                                 .ConfigureAwait(false);
        }

        public override async Task<Track> MostPlayedTrack(int userId)
        {
            var sql = @"SELECT t.*
                        FROM `usertrack` ut
                        join `track` t on (ut.trackId = t.id)
                        WHERE ut.userId = {0}
                        GROUP by t.id
                        ORDER by SUM(ut.playedCount) desc
                        LIMIT 1";
            return await Tracks.FromSqlRaw(sql, userId)
                               .Include(x => x.TrackArtist)
                               .Include(x => x.ReleaseMedia)
                               .Include("ReleaseMedia.Release")
                               .Include("ReleaseMedia.Release.Artist")
                               .FirstOrDefaultAsync()
                               .ConfigureAwait(false);
        }

        public override async Task<SortedDictionary<int, int>> RandomArtistIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var sql = @"SELECT a.id
                        FROM `artist` a
                        WHERE(a.id NOT IN(select artistId FROM `userartist` where userId = {1} and isDisliked = 1))
                        OR(a.id IN(select artistId FROM `userartist` where userId = {1} and isFavorite = 1)
                        AND {2} = 1)
                        order BY RIGHT(HEX((1 << 24) * (1 + RAND())), 6)
                        LIMIT 0, {0}";
            var ids = await Artists.FromSqlRaw(sql, randomLimit, userId, doOnlyFavorites ? "1" : "0").Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomGenreIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var sql = @"SELECT g.id
                        FROM `genre` g
                        ORDER BY RIGHT( HEX( (1<<24) * (1+RAND()) ), 6)
                        LIMIT 0, {0}";
            var ids = await Genres.FromSqlRaw(sql, randomLimit).Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomLabelIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var sql = @"SELECT l.id
                        FROM `label` l
                        ORDER BY RIGHT( HEX( (1<<24) * (1+RAND()) ), 6)
                        LIMIT 0, {0}";
            var ids = await Labels.FromSqlRaw(sql, randomLimit).Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomReleaseIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var sql = @"SELECT r.id
                        FROM `release` r
                        WHERE (r.id NOT IN (select releaseId FROM `userrelease` where userId = {1} and isDisliked = 1))
                        OR (r.id IN (select releaseId FROM `userrelease` where userId = {1} and isFavorite = 1)
                            AND {2} = 1)
                        ORDER BY RIGHT( HEX( (1<<24) * (1+RAND()) ), 6)
                        LIMIT 0, {0}";
            var ids = await Releases.FromSqlRaw(sql, randomLimit, userId, doOnlyFavorites ? "1" : "0").Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }

        public override async Task<SortedDictionary<int, int>> RandomTrackIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false)
        {
            var sql = @"SELECT t.id
                        FROM `track` t
                        # Rated filter
                        WHERE ((t.rating > 0 AND {3} = 1) OR {3} = 0)
                        # Artist and TrackArtist is not disliked
                        AND ((t.id NOT IN (select tt.id
                                            FROM `track` tt
                                            JOIN `releasemedia` rm on (tt.releaseMediaId = rm.id)
                                            JOIN `release` r on (rm.releaseId = r.id)
                                            JOIN `userartist` ua on (r.artistId = ua.artistId)
                                            WHERE ua.userId = {1} AND ua.isDisliked = 1
                                            UNION
                                            select tt.id
                                            FROM `track` tt
                                            JOIN `userartist` ua on (tt.artistId = ua.artistId)
                                            WHERE ua.userId = {1} AND ua.isDisliked = 1))
                            # Release is not disliked
                            AND (t.id NOT IN (select tt.id
                                            FROM `track` tt
                                            JOIN `releasemedia` rm on (tt.releaseMediaId = rm.id)
                                            JOIN `userrelease` ur on (rm.releaseId = ur.releaseId)
                                            WHERE ur.userId = {1} AND ur.isDisliked = 1))
                            # Track is not disliked
                            AND (t.id NOT IN (select tt.id
                                            FROM `track` tt
                                            JOIN `usertrack` ut on (tt.id = ut.trackId)
                                            WHERE ut.userId = {1} AND ut.isDisliked = 1)))
                        # If toggled then only favorites
                        AND ((t.id IN (select tt.id
                                        FROM `track` tt
			                            JOIN `usertrack` ut on (tt.id = ut.trackId)
	                                    WHERE ut.userId = {1} AND ut.isFavorite = 1) AND {2} = 1) OR {2} = 0)
                        order BY RIGHT( HEX( (1<<24) * (1+RAND()) ), 6)
                        LIMIT 0, {0}";
            var ids = await Tracks.FromSqlRaw(sql, randomLimit, userId, doOnlyFavorites ? "1" : "0", doOnlyRated ? "1" : "0").Select(x => x.Id).ToListAsync().ConfigureAwait(false);
            var dict = ids.Select((id, i) => new { key = i, value = id }).ToDictionary(x => x.key, x => x.value);
            return new SortedDictionary<int, int>(dict);
        }
    }
}