using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Threading.Tasks;
using User = Roadie.Library.Models.Users.User;

namespace Roadie.Api.Services
{
    public interface ISubsonicService
    {
        Task<SubsonicOperationResult<Response>> AddChatMessageAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<SubsonicAuthenticateResponse>> AuthenticateAsync(Request request);

        Task<SubsonicOperationResult<Response>> CreateBookmarkAsync(Request request, User roadieUser, int position, string comment);

        Task<SubsonicOperationResult<Response>> CreatePlaylistAsync(Request request, User roadieUser, string name, string[] songIds, string playlistId = null);

        Task<SubsonicOperationResult<Response>> DeleteBookmarkAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> DeletePlaylistAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetAlbumAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetAlbumInfoAsync(Request request, User roadieUser, AlbumInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetAlbumListAsync(Request request, User roadieUser, AlbumListVersions version);

        Task<SubsonicOperationResult<Response>> GetArtistAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetArtistInfoAsync(Request request, int? count, bool includeNotPresent, ArtistInfoVersion version);

        Task<SubsonicOperationResult<Response>> GetArtistsAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetBookmarksAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetChatMessagesAsync(Request request, User roadieUser, long? since);

        Task<SubsonicFileOperationResult<Library.Models.Image>> GetCoverArtAsync(Request request, int? size);

        Task<SubsonicOperationResult<Response>> GetGenresAsync(Request request);

        Task<SubsonicOperationResult<Response>> GetIndexesAsync(Request request, User roadieUser, long? ifModifiedSince = null);

        SubsonicOperationResult<Response> GetLicense(Request request);

        SubsonicOperationResult<Response> GetLyrics(Request request, string artistId, string title);

        Task<SubsonicOperationResult<Response>> GetMusicDirectoryAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetMusicFoldersAsync(Request request);

        Task<SubsonicOperationResult<Response>> GetNowPlayingAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetPlaylistAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetPlaylistsAsync(Request request, User roadieUser, string filterToUserName);

        Task<SubsonicOperationResult<Response>> GetPlayQueueAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetPodcastsAsync(Request request);

        Task<SubsonicOperationResult<Response>> GetRandomSongsAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetSimliarSongsAsync(Request request, User roadieUser, SimilarSongsVersion version, int? count = 50);

        Task<SubsonicOperationResult<Response>> GetSongAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetSongsByGenreAsync(Request request, User roadieUser);

        Task<SubsonicOperationResult<Response>> GetStarredAsync(Request request, User roadieUser, StarredVersion version);

        Task<SubsonicOperationResult<Response>> GetTopSongsAsync(Request request, User roadieUser, int? count = 50);

        Task<SubsonicOperationResult<Response>> GetUserAsync(Request request, string username);

        SubsonicOperationResult<Response> GetVideos(Request request);

        SubsonicOperationResult<Response> Ping(Request request);

        Task<SubsonicOperationResult<Response>> SavePlayQueueAsync(Request request, User roadieUser, string current, long? position);

        Task<SubsonicOperationResult<Response>> SearchAsync(Request request, User roadieUser, SearchVersion version);

        Task<SubsonicOperationResult<Response>> SetRatingAsync(Request request, User roadieUser, short rating);

        Task<SubsonicOperationResult<Response>> ToggleStarAsync(Request request, User roadieUser, bool star, string[] albumIds = null, string[] artistIds = null);

        Task<SubsonicOperationResult<Response>> UpdatePlaylistAsync(Request request, User roadieUser, string playlistId,
            string name = null, string comment = null, bool? isPublic = null, string[] songIdsToAdd = null,
            int[] songIndexesToRemove = null);
    }
}