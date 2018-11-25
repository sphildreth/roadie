using Roadie.Library.Identity;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ISubsonicService
    {
        Task<SubsonicOperationResult<SubsonicAuthenticateResponse>> Authenticate(Request request);

        Task<SubsonicOperationResult<Response>> CreateBookmark(Request request, Roadie.Library.Models.Users.User roadieUser, int position, string comment);
        Task<SubsonicOperationResult<Response>> CreatePlaylist(Request request, Roadie.Library.Models.Users.User roadieUser, string name, string[] songIds, string playlistId = null);

        Task<SubsonicOperationResult<Response>> DeleteBookmark(Request request, Roadie.Library.Models.Users.User roadieUser);
        Task<SubsonicOperationResult<Response>> DeletePlaylist(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetAlbum(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetAlbumInfo(Request request, Roadie.Library.Models.Users.User roadieUser, AlbumInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetAlbumList(Request request, Roadie.Library.Models.Users.User roadieUser, AlbumListVersions version);

        Task<SubsonicOperationResult<Response>> GetArtist(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetArtistInfo(Request request, int? count, bool includeNotPresent, ArtistInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetArtists(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetBookmarks(Request request, Roadie.Library.Models.Users.User roadieUser);

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

        Task<SubsonicOperationResult<Response>> GetSimliarSongs(Request request, Roadie.Library.Models.Users.User roadieUser, SimilarSongsVersion version, int? count = 50);

        Task<SubsonicOperationResult<Response>> GetSong(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetSongsByGenre(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> GetStarred(Request request, Roadie.Library.Models.Users.User roadieUser, StarredVersion version);

        Task<SubsonicOperationResult<Response>> GetTopSongs(Request request, Roadie.Library.Models.Users.User roadieUser, int? count = 50);

        Task<SubsonicOperationResult<Response>> GetUser(Request request, string username);

        SubsonicOperationResult<Response> GetVideos(Request request);

        SubsonicOperationResult<Response> Ping(Request request);

        Task<SubsonicOperationResult<Response>> GetNowPlaying(Request request, Roadie.Library.Models.Users.User roadieUser);

        Task<SubsonicOperationResult<Response>> Search(Request request, Roadie.Library.Models.Users.User roadieUser, SearchVersion version);

        Task<SubsonicOperationResult<Response>> ToggleStar(Request request, Roadie.Library.Models.Users.User roadieUser, bool star, string[] albumIds = null, string[] artistIds = null);
        Task<SubsonicOperationResult<Response>> SetRating(Request request, Roadie.Library.Models.Users.User roadieUser, short rating);

        Task<SubsonicOperationResult<Response>> UpdatePlaylist(Request request, Roadie.Library.Models.Users.User roadieUser, string playlistId, string name = null, string comment = null, bool? isPublic = null, string[] songIdsToAdd = null, int[] songIndexesToRemove = null);
    }
}