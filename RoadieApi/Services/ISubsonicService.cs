using Roadie.Library;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ISubsonicService
    {
        Task<OperationResult<Response>> GetAlbumList(Request request, Roadie.Library.Models.Users.User roadieUser, string version);

        Task<FileOperationResult<Roadie.Library.Models.Image>> GetCoverArt(Request request, int? size);

        Task<OperationResult<Response>> GetGenres(Request request);

        Task<OperationResult<Response>> GetIndexes(Request request, Roadie.Library.Models.Users.User roadieUser, string musicFolderId = null, long? ifModifiedSince = null);

        Task<OperationResult<Response>> GetMusicDirectory(Request request, Roadie.Library.Models.Users.User roadieUser, string id);

        Task<OperationResult<Response>> GetMusicFolders(Request request);

        Task<OperationResult<Response>> GetPlaylists(Request request, Roadie.Library.Models.Users.User roadieUser, string filterToUserName);

        Task<OperationResult<Response>> GetPodcasts(Request request);

        OperationResult<Response> Ping(Request request);

        OperationResult<Response> GetLicense(Request request);

        Task<OperationResult<Response>> Search(Request request, Roadie.Library.Models.Users.User roadieUser);
        Task<OperationResult<Response>> GetAlbum(Request request, Roadie.Library.Models.Users.User roadieUser);
    }
}