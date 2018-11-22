using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ISubsonicService
    {
        Task<SubsonicOperationResult<Response>> GetAlbum(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetAlbumList(Request request, Roadie.Library.Models.Users.User roadieUser, AlbumListVersions version);

        Task<SubsonicOperationResult<Response>> GetArtistInfo(Request request, int? count, bool includeNotPresent, ArtistInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetArtists(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicFileOperationResult<Roadie.Library.Models.Image>> GetCoverArt(Request request, int? size);

        Task<SubsonicOperationResult<Response>> GetGenres(Request request);

        Task<SubsonicOperationResult<Response>> GetIndexes(Request request, Roadie.Library.Models.Users.User roadieUser, long? ifModifiedSince = null);

        SubsonicOperationResult<Response> GetLicense(Request request);

        SubsonicOperationResult<Response> GetLyrics(Request request, string artistId, string title);

        Task<SubsonicOperationResult<Response>> GetMusicDirectory(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetMusicFolders(Request request);

        Task<SubsonicOperationResult<Response>> GetPlaylist(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetPlaylists(Request request, Roadie.Library.Models.Users.User roadieUser, string filterToUserName);

        Task<SubsonicOperationResult<Response>> GetPodcasts(Request request);

        Task<SubsonicOperationResult<Response>> GetRandomSongs(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetStarred(Request request, Roadie.Library.Models.Users.User roadieUser, StarredVersion version);

        Task<SubsonicOperationResult<Response>> GetUser(Request request, string username);

        SubsonicOperationResult<Response> GetVideos(Request request);

        SubsonicOperationResult<Response> Ping(Request request);

        Task<SubsonicOperationResult<Response>> Search(Request request, Roadie.Library.Models.Users.User roadieUser, SearchVersion version);

        Task<SubsonicOperationResult<Response>> GetAlbumInfo(Request request, Roadie.Library.Models.Users.User roadieUser, AlbumInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetArtist(Request request, Roadie.Library.Models.Users.User roadieUser);
        Task<SubsonicOperationResult<Response>> GetSong(Request request, Roadie.Library.Models.Users.User roadieUser);
    }
}