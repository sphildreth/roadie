using Roadie.Library.Models;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Threading.Tasks;
using User = Roadie.Library.Models.Users.User;

namespace Roadie.Api.Services
{
    public interface ISubsonicService
    {
        Task<SubsonicOperationResult<Response>> AddChatMessage(Request request, User roadieUser);

        Task<SubsonicOperationResult<SubsonicAuthenticateResponse>> Authenticate(Request request);

        Task<SubsonicOperationResult<Response>> CreateBookmark(Request request, User roadieUser, int position,
            string comment);

        Task<SubsonicOperationResult<Response>> CreatePlaylist(Request request, User roadieUser, string name,
            string[] songIds, string playlistId = null);

        Task<SubsonicOperationResult<Response>> DeleteBookmark(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> DeletePlaylist(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetAlbum(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>>
            GetAlbumInfo(Request request, User roadieUser, AlbumInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetAlbumList(Request request, User roadieUser,
            AlbumListVersions version);

        Task<SubsonicOperationResult<Response>> GetArtist(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetArtistInfo(Request request, int? count, bool includeNotPresent,
            ArtistInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetArtists(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetBookmarks(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetChatMessages(Request request, User roadieUser, long? since);

        Task<SubsonicFileOperationResult<Image>> GetCoverArt(Request request, int? size);

        Task<SubsonicOperationResult<Response>> GetGenres(Request request);

        Task<SubsonicOperationResult<Response>> GetIndexes(Request request, User roadieUser,
            long? ifModifiedSince = null);

        SubsonicOperationResult<Response> GetLicense(Request request);

        SubsonicOperationResult<Response> GetLyrics(Request request, string artistId, string title);

        Task<SubsonicOperationResult<Response>> GetMusicDirectory(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetMusicFolders(Request request);

        Task<SubsonicOperationResult<Response>> GetNowPlaying(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetPlaylist(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetPlaylists(Request request, User roadieUser, string filterToUserName);

        Task<SubsonicOperationResult<Response>> GetPlayQueue(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetPodcasts(Request request);

        Task<SubsonicOperationResult<Response>> GetRandomSongs(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetSimliarSongs(Request request, User roadieUser,
            SimilarSongsVersion version, int? count = 50);

        Task<SubsonicOperationResult<Response>> GetSong(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetSongsByGenre(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetStarred(Request request, User roadieUser, StarredVersion version);

        Task<SubsonicOperationResult<Response>> GetTopSongs(Request request, User roadieUser, int? count = 50);

        Task<SubsonicOperationResult<Response>> GetUser(Request request, string username);

        SubsonicOperationResult<Response> GetVideos(Request request);

        SubsonicOperationResult<Response> Ping(Request request);

        Task<SubsonicOperationResult<Response>> SavePlayQueue(Request request, User roadieUser, string current,
            long? position);

        Task<SubsonicOperationResult<Response>> Search(Request request, User roadieUser, SearchVersion version);

        Task<SubsonicOperationResult<Response>> SetRating(Request request, User roadieUser, short rating);

        Task<SubsonicOperationResult<Response>> ToggleStar(Request request, User roadieUser, bool star,
            string[] albumIds = null, string[] artistIds = null);

        Task<SubsonicOperationResult<Response>> UpdatePlaylist(Request request, User roadieUser, string playlistId,
            string name = null, string comment = null, bool? isPublic = null, string[] songIdsToAdd = null,
            int[] songIndexesToRemove = null);
    }
}