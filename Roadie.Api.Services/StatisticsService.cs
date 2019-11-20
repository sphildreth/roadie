using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
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
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<StatisticsService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public Task<OperationResult<LibraryStats>> LibraryStatistics()
        {
            LibraryStats result = null;
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                result = new LibraryStats
                {
                    UserCount = DbContext.Users.Count(),
                    CollectionCount = DbContext.Collections.Count(),
                    PlaylistCount = DbContext.Playlists.Count(),
                    ArtistCount = DbContext.Artists.Count(),
                    LabelCount = DbContext.Labels.Count(),
                    ReleaseCount = DbContext.Releases.Count(),
                    ReleaseMediaCount = DbContext.ReleaseMedias.Count()
                };
                result.PlayedCount = DbContext.UserTracks.Sum(x => x.PlayedCount);
                var tracks = DbContext.Tracks.Where(x => x.Hash != null);
                result.TrackCount = tracks.Count();
                result.TotalTrackDuration = tracks.Sum(x => (long?)x.Duration);
                result.TotalTrackSize = tracks.Sum(x => (long?)x.FileSize);
                var lastScan = DbContext.ScanHistories.OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                result.LastScan = lastScan?.CreatedDate;
                sw.Stop();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return Task.FromResult(new OperationResult<LibraryStats>
            {
                OperationTime = sw.ElapsedMilliseconds,
                IsSuccess = result != null,
                Data = result
            });
        }

        public Task<OperationResult<IEnumerable<DateAndCount>>> ArtistsByDate()
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new List<DateAndCount>();
            var dateInfos = (from r in DbContext.Artists
                             orderby r.CreatedDate
                             select r.CreatedDate)
                             .ToArray()
                             .GroupBy(x => x.ToString("yyyy-MM-dd"))
                             .Select(x => new
                             {
                                 date = x.Key,
                                 count = x.Count()
                             });
            foreach (var dateInfo in dateInfos)
            {
                result.Add(new DateAndCount
                {
                    Date = dateInfo.date,
                    Count = dateInfo.count
                });
            }
            sw.Stop();
            return Task.FromResult(new OperationResult<IEnumerable<DateAndCount>>
            {
                OperationTime = sw.ElapsedMilliseconds,
                IsSuccess = result != null,
                Data = result
            });
        }

        public Task<OperationResult<IEnumerable<DateAndCount>>> ReleasesByDate()
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new List<DateAndCount>();
            var dateInfos = (from r in DbContext.Releases
                             orderby r.CreatedDate
                             select r.CreatedDate)
                             .ToArray()
                             .GroupBy(x => x.ToString("yyyy-MM-dd"))
                             .Select(x => new
                             {
                                 date = x.Key,
                                 count = x.Count()
                             });
            foreach (var dateInfo in dateInfos)
            {
                result.Add(new DateAndCount
                {
                    Date = dateInfo.date,
                    Count = dateInfo.count
                });
            }
            sw.Stop();
            return Task.FromResult(new OperationResult<IEnumerable<DateAndCount>>
            {
                OperationTime = sw.ElapsedMilliseconds,
                IsSuccess = result != null,
                Data = result
            });
        }

        public Task<OperationResult<IEnumerable<DateAndCount>>> SongsPlayedByUser()
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new List<DateAndCount>();
            var dateInfos = (from r in DbContext.UserTracks
                             join u in DbContext.Users on r.UserId equals u.Id
                             select new { u.UserName, r.PlayedCount })
                             .ToArray()
                             .GroupBy(x => x.UserName)
                             .Select(x => new
                             {
                                 username = x.Key,
                                 count = x.Count()
                             });
            foreach (var dateInfo in dateInfos)
            {
                result.Add(new DateAndCount
                {
                    Date = dateInfo.username,
                    Count = dateInfo.count
                });
            }
            sw.Stop();
            return Task.FromResult(new OperationResult<IEnumerable<DateAndCount>>
            {
                OperationTime = sw.ElapsedMilliseconds,
                IsSuccess = result != null,
                Data = result
            });
        }

        public Task<OperationResult<IEnumerable<DateAndCount>>> SongsPlayedByDate()
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new List<DateAndCount>();
            var dateInfos = (from r in DbContext.UserTracks
                             orderby r.LastPlayed
                             select r.LastPlayed ?? r.CreatedDate)
                             .ToArray()
                             .GroupBy(x => x.ToString("yyyy-MM-dd"))
                             .Select(x => new
                             {
                                 date = x.Key,
                                 count = x.Count()
                             });
            foreach (var dateInfo in dateInfos)
            {
                result.Add(new DateAndCount
                {
                    Date = dateInfo.date,
                    Count = dateInfo.count
                });
            }
            sw.Stop();
            return Task.FromResult(new OperationResult<IEnumerable<DateAndCount>>
            {
                OperationTime = sw.ElapsedMilliseconds,
                IsSuccess = result != null,
                Data = result
            });
        }

        public Task<OperationResult<IEnumerable<DateAndCount>>> ReleasesByDecade()
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new List<DateAndCount>();
            var decadeInfos = (from r in DbContext.Releases
                             orderby r.ReleaseDate
                             select r.ReleaseDate ?? r.CreatedDate)
                             .ToArray()
                             .GroupBy(x => x.ToString("yyyy"))
                             .Select(x => new
                             {
                                 year = SafeParser.ToNumber<int>(x.Key),
                                 count = x.Count()
                             });
            if (decadeInfos != null && decadeInfos.Any())
            {
                var decadeInterval = 10;
                var startingDecade = (decadeInfos.Min(x => x.year) / 10) * 10;
                var endingDecade = (decadeInfos.Max(x => x.year) / 10) * 10;
                for (int decade = startingDecade; decade <= endingDecade; decade += decadeInterval)
                {
                    var endOfDecade = decade + 9;
                    var count = decadeInfos.Where(x => x.year >= decade && x.year <= endOfDecade).Sum(x => x.count);
                    if (count > 0)
                    {
                        result.Add(new DateAndCount
                        {
                            Date = decade.ToString(),
                            Count = count
                        });
                    }
                }
            }
            sw.Stop();
            return Task.FromResult(new OperationResult<IEnumerable<DateAndCount>>
            {
                OperationTime = sw.ElapsedMilliseconds,
                IsSuccess = result != null,
                Data = result
            });
        }


    }
}