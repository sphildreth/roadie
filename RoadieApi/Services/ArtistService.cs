using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class ArtistService : ServiceBase, IArtistService
    {
        public ArtistService(IRoadieSettings configuration, IHttpEncoder httpEncoder, data.IRoadieDbContext context,
                             ICacheManager cacheManager, ILogger logger)
            : base(configuration, httpEncoder, context, cacheManager, logger)
        {
        }

        public async Task<OperationResult<bool>> AddArtist(Artist artist)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<Artist>> ArtistById(Guid id, IEnumerable<string> includes)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<Artist>> ArtistByName(string name, IEnumerable<string> includes)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> DeleteArtist(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> DeleteArtistReleases(Guid roadieId)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<ArtistList>> List(PagedRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> MergeArtists(Guid artistId, Guid mergeInfoArtistId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> MergeReleases(Guid artistId, string releaseIdToMerge, string releaseIdToMergeInto, bool addAsMedia)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> RefreshArtistMetaData(Guid artistId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> RescanArtist(Guid artistId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetImage(Guid id, byte[] imageBytes)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetImageByImageId(Guid artistId, Guid imageId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetImageViaUrl(Guid id, string imageUrl)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetUserRating(Guid artistId, Guid userId, short rating)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> ToggleUserDislikeArtist(Guid artistId, Guid UserId, bool dislike)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> ToggleUserFavoriteArtist(Guid artistId, Guid UserId, bool favorite)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> UpdateArtist(Artist ea)
        {
            throw new NotImplementedException();
        }
    }
}