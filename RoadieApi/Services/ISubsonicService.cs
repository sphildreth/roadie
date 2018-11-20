using Roadie.Library;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ISubsonicService
    {
        Task<OperationResult<Response>> GetGenres(Request request);

        Task<OperationResult<Response>> GetIndexes(Request request, string musicFolderId = null, long? ifModifiedSince = null);

        Task<OperationResult<Response>> GetMusicFolders(Request request);

        Task<OperationResult<Response>> GetPlaylists(Request request, Roadie.Library.Models.Users.User roadieUser, string filterToUserName);

        Task<OperationResult<Response>> GetPodcasts(Request request);

        Task<OperationResult<Response>> GetMusicDirectory(Request request, Roadie.Library.Models.Users.User roadieUser, string id);

        Task<OperationResult<Response>> GetAlbumList(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<FileOperationResult<Roadie.Library.Models.Image>> GetCoverArt(Request request, int? size);

        OperationResult<Response> Ping(Request request);
    }
}