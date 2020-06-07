using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Models;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    /// <summary>
    ///     Returns lookups (or dictionaries) of various allowable values for a given Type
    /// </summary>
    public class LookupService : ServiceBase, ILookupService
    {
        public const string CreditCategoriesCacheKey = "urn:creditCategories";

        public LookupService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<PlaylistService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
        }

        public Task<OperationResult<IEnumerable<DataToken>>> ArtistTypesAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(ArtistType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> BandStatusAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(BandStatus)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> BookmarkTypesAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(BookmarkType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> CollectionTypesAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(CollectionType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> LibraryStatusAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(LibraryStatus)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> QueMessageTypesAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(QueMessageType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> ReleaseTypesAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(ReleaseType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> RequestStatusAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(RequestStatus)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public async Task<OperationResult<IEnumerable<DataToken>>> CreditCategoriesAsync()
        {
            var sw = Stopwatch.StartNew();
            var data = await CacheManager.GetAsync(CreditCategoriesCacheKey, async () =>
            {
                return (await DbContext.CreditCategory.ToListAsync().ConfigureAwait(false)).Select(x => new DataToken
                {
                    Value = x.RoadieId.ToString(),
                    Text = x.Name
                }).ToArray();
            }, CacheManagerBase.SystemCacheRegionUrn).ConfigureAwait(false);
            return new OperationResult<IEnumerable<DataToken>>
            {
                Data = data,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public Task<OperationResult<IEnumerable<DataToken>>> StatusAsync()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(Statuses)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        private IEnumerable<DataToken> EnumToDataTokens(Type ee)
        {
            var result = new List<DataToken>();
            foreach (var ls in Enum.GetValues(ee))
                result.Add(new DataToken
                {
                    Text = ls.ToString(),
                    Value = ((short)ls).ToString()
                });
            return result.OrderBy(x => x.Text);
        }
    }
}