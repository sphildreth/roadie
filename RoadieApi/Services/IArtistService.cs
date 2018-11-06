using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;

namespace Roadie.Api.Services
{
    public interface IArtistService
    {
        Task<OperationResult<bool>> AddArtist(Artist artist);
        Task<OperationResult<Artist>> ArtistById(Guid id, IEnumerable<string> includes);
        Task<OperationResult<Artist>> ArtistByName(string name, IEnumerable<string> includes);
        Task<OperationResult<bool>> DeleteArtist(Guid id);
        Task<OperationResult<bool>> DeleteArtistReleases(Guid roadieId);
        Task<PagedResult<ArtistList>> List(PagedRequest request);
        Task<OperationResult<bool>> MergeArtists(Guid artistId, Guid mergeInfoArtistId);
        Task<OperationResult<bool>> MergeReleases(Guid artistId, string releaseIdToMerge, string releaseIdToMergeInto, bool addAsMedia);
        Task<OperationResult<bool>> RefreshArtistMetaData(Guid artistId);
        Task<OperationResult<bool>> RescanArtist(Guid artistId);
        Task<OperationResult<bool>> SetImage(Guid id, byte[] imageBytes);
        Task<OperationResult<bool>> SetImageByImageId(Guid artistId, Guid imageId);
        Task<OperationResult<bool>> SetImageViaUrl(Guid id, string imageUrl);
        Task<OperationResult<bool>> SetUserRating(Guid artistId, Guid userId, short rating);
        Task<OperationResult<bool>> ToggleUserDislikeArtist(Guid artistId, Guid UserId, bool dislike);
        Task<OperationResult<bool>> ToggleUserFavoriteArtist(Guid artistId, Guid UserId, bool favorite);
        Task<OperationResult<bool>> UpdateArtist(Artist ea);
    }
}