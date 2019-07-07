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
                    PlayedCount = DbContext.Playlists.Count(),
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

        public Task<OperationResult<IEnumerable<DateAndCount>>> ReleasesByDate()
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new List<DateAndCount>();
            var dateInfos = (from r in DbContext.Releases
                             orderby r.CreatedDate
                             group r by r.CreatedDate.ToString("yyyy-MM-dd") into g
                             select new
                             {
                                 date = g.Key,
                                 count = g.Count()
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
    }
}