using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class StatisticsService : ServiceBase, IStatisticsService
    {
        public StatisticsService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            data.IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<StatisticsService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<OperationResult<LibraryStats>> LibraryStatistics()
        {
            LibraryStats result = null;
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                using (var conn = new MySqlConnection(Configuration.ConnectionString))
                {
                    conn.Open();
                    var sql = @"SELECT rm.releaseMediaCount AS releaseMediaCount, COUNT(r.roadieId) AS releaseCount,
                                ts.trackCount, ts.trackDuration as TotalTrackDuration, ts.trackSize as TotalTrackSize, ac.artistCount, lc.labelCount, pc.playedCount, uc.userCount, cc.collectionCount, pl.playlistCount
                            FROM `release` r
                            INNER JOIN (
	                            SELECT COUNT(1) AS trackCount, SUM(t.duration) AS trackDuration, SUM(t.fileSize) AS trackSize
	                            FROM `track` t
	                            JOIN `releasemedia` rm ON rm.id = t.releaseMediaId
	                            JOIN `release` r ON r.id = rm.releaseId
	                            JOIN `artist` a ON a.id = r.artistId
	                            WHERE t.hash IS NOT NULL) ts
                            INNER JOIN (
	                            SELECT COUNT(1) AS artistCount
	                            FROM `artist`) ac
                            INNER JOIN (
	                            SELECT COUNT(1) AS labelCount
	                            FROM `label`) lc
                            INNER JOIN (
	                            SELECT SUM(playedCount) as playedCount
	                            FROM `usertrack`) pc
                            INNER JOIN (
	                            SELECT COUNT(1) as releaseMediaCount
	                            FROM `releasemedia`) rm
                            INNER JOIN (
	                            SELECT COUNT(1) as userCount
	                            FROM `user`) uc
                            INNER JOIN (
	                            SELECT COUNT(1) as collectionCount
	                            FROM `collection`) cc
                            INNER JOIN (
	                            SELECT COUNT(1) as playlistCount
	                            FROM `playlist`) pl;";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        try
                        {
                            using (var rdr = await cmd.ExecuteReaderAsync())
                            {
                                if (rdr.HasRows)
                                    while (rdr.Read())
                                        result = new LibraryStats
                                        {
                                            UserCount = SafeParser.ToNumber<int?>(rdr["UserCount"]),
                                            CollectionCount = SafeParser.ToNumber<int?>(rdr["CollectionCount"]),
                                            PlaylistCount = SafeParser.ToNumber<int?>(rdr["PlaylistCount"]),
                                            ArtistCount = SafeParser.ToNumber<int?>(rdr["ArtistCount"]),
                                            LabelCount = SafeParser.ToNumber<int?>(rdr["LabelCount"]),
                                            ReleaseCount = SafeParser.ToNumber<int?>(rdr["ReleaseCount"]),
                                            ReleaseMediaCount = SafeParser.ToNumber<int?>(rdr["ReleaseMediaCount"]),
                                            PlayedCount = SafeParser.ToNumber<int?>(rdr["PlayedCount"]),
                                            TrackCount = SafeParser.ToNumber<int?>(rdr["TrackCount"]),
                                            TotalTrackDuration = SafeParser.ToNumber<long?>(rdr["TotalTrackDuration"]),
                                            TotalTrackSize = SafeParser.ToNumber<long?>(rdr["TotalTrackSize"])
                                        };
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }

                var lastScan = DbContext.ScanHistories.OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                if (lastScan != null) result.LastScan = lastScan.CreatedDate;
                sw.Stop();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return new OperationResult<LibraryStats>
            {
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds,
                Data = result
            };
        }

        public Task<OperationResult<IEnumerable<DateAndCount>>> ReleasesByDate()
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = new List<DateAndCount>();

            using (var conn = new MySqlConnection(Configuration.ConnectionString))
            {
                conn.Open();
                var sql = @"SELECT DATE_FORMAT(createdDate, '%Y-%m-%d') as date, count(1) as count
                            FROM `release`
                            group by DATE_FORMAT(createdDate, '%Y-%m-%d')
                            order by createdDate;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    try
                    {
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                                while (rdr.Read())
                                    result.Add(new DateAndCount
                                    {
                                        Date = SafeParser.ToString(rdr["date"]),
                                        Count = SafeParser.ToNumber<int?>(rdr["count"])
                                    });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            sw.Stop();
            return Task.FromResult(new OperationResult<IEnumerable<DateAndCount>>
            {
                OperationTime = sw.ElapsedMilliseconds,
                Data = result
            });
        }
    }
}