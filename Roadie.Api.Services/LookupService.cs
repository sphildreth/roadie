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
        public LookupService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<PlaylistService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
        }

        public Task<OperationResult<IEnumerable<DataToken>>> ArtistTypes()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(ArtistType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> BandStatus()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(BandStatus)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> BookmarkTypes()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(BookmarkType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> CollectionTypes()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(CollectionType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> LibraryStatus()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(LibraryStatus)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> QueMessageTypes()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(QueMessageType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> ReleaseTypes()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(ReleaseType)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> RequestStatus()
        {
            var sw = Stopwatch.StartNew();
            return Task.FromResult(new OperationResult<IEnumerable<DataToken>>
            {
                Data = EnumToDataTokens(typeof(RequestStatus)),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public Task<OperationResult<IEnumerable<DataToken>>> Status()
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