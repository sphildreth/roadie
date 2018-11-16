using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class LabelService : ServiceBase, ILabelService
    {
        public LabelService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<LabelList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }
            var result = (from l in this.DbContext.Labels
                          let artistCount = (from lb in this.DbContext.Labels
                                             join rll in this.DbContext.ReleaseLabels on lb.Id equals rll.LabelId into rldd
                                             from rll in rldd.DefaultIfEmpty()
                                             join rr in this.DbContext.Releases on rll.ReleaseId equals rr.Id
                                             join aa in this.DbContext.Artists on rr.ArtistId equals aa.Id
                                             where lb.Id == l.Id
                                             select aa.Id).Count()
                          let releaseCount = (from lbb in this.DbContext.Labels
                                              join rlll in this.DbContext.ReleaseLabels on lbb.Id equals rlll.LabelId into rlddd
                                              from rlll in rlddd.DefaultIfEmpty()
                                              join rrr in this.DbContext.Releases on rlll.ReleaseId equals rrr.Id
                                              where lbb.Id == l.Id
                                              select rrr.Id).Count()
                          let trackCount = (from lbtc in this.DbContext.Labels
                                            join rlltc in this.DbContext.ReleaseLabels on lbtc.Id equals rlltc.LabelId into rlddtc
                                            from rlltc in rlddtc.DefaultIfEmpty()
                                            join rrtc in this.DbContext.Releases on rlltc.ReleaseId equals rrtc.Id
                                            join rmtc in this.DbContext.ReleaseMedias on rrtc.Id equals rmtc.ReleaseId
                                            join tttc in this.DbContext.Tracks on rmtc.Id equals tttc.ReleaseMediaId
                                            where lbtc.Id == l.Id
                                            select tttc.Id).Count()
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (
                                    l.Name != null && l.Name.Contains(request.FilterValue) ||
                                    l.AlternateNames != null && l.AlternateNames.Contains(request.FilterValue)
                          )))
                          select new LabelList
                          {
                              DatabaseId = l.Id,
                              Id = l.RoadieId,
                              Label = new DataToken
                              {
                                  Text = l.Name,
                                  Value = l.RoadieId.ToString()
                              },
                              SortName = l.SortName,
                              CreatedDate = l.CreatedDate,
                              LastUpdated = l.LastUpdated,
                              ArtistCount = artistCount,
                              ReleaseCount = releaseCount,
                              TrackCount = trackCount,
                              Thumbnail = this.MakeLabelThumbnailImage(l.RoadieId)
                          });
            var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Label.Text", "ASC" } }) : request.OrderValue(null);
            var rowCount = result.Count();
            var rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<LabelList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            };
        }
    }
}