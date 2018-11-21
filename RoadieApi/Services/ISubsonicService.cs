using Roadie.Library;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ISubsonicService
    {
        Task<OperationResult<Response>> GetAlbum(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<OperationResult<Response>> GetAlbumList(Request request, Roadie.Library.Models.Users.User roadieUser, AlbumListVersions version);

        Task<OperationResult<Response>> GetArtistInfo(Request request, string id, int? count, bool includeNotPresent, ArtistInfoVersion version);

        Task<OperationResult<Response>> GetArtists(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<FileOperationResult<Roadie.Library.Models.Image>> GetCoverArt(Request request, int? size);

        Task<OperationResult<Response>> GetGenres(Request request);

        Task<OperationResult<Response>> GetIndexes(Request request, Roadie.Library.Models.Users.User roadieUser, string musicFolderId = null, long? ifModifiedSince = null);

        OperationResult<Response> GetLicense(Request request);

        Task<OperationResult<Response>> GetMusicDirectory(Request request, Roadie.Library.Models.Users.User roadieUser, string id);

        Task<OperationResult<Response>> GetMusicFolders(Request request);

        Task<OperationResult<Response>> GetPlaylist(Request request, Roadie.Library.Models.Users.User roadieUser, string id);

        Task<OperationResult<Response>> GetPlaylists(Request request, Roadie.Library.Models.Users.User roadieUser, string filterToUserName);

        Task<OperationResult<Response>> GetPodcasts(Request request);

        Task<OperationResult<Response>> GetRandomSongs(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<OperationResult<Response>> GetStarred(Request request, Roadie.Library.Models.Users.User roadieUser, StarredVersion version);

        Task<OperationResult<Response>> GetUser(Request request, string username);

        OperationResult<Response> Ping(Request request);

        Task<OperationResult<Response>> Search(Request request, Roadie.Library.Models.Users.User roadieUser, SearchVersion version);
    }
}