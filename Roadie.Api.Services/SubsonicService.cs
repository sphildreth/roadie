using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Releases;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using data = Roadie.Library.Data;
using subsonic = Roadie.Library.Models.ThirdPartyApi.Subsonic;

namespace Roadie.Api.Services
{
    /// <summary>
    ///     Subsonic API emulator for Roadie. Enables Subsonic clients to work with Roadie.
    ///     <seealso cref="http://www.subsonic.org/pages/inc/api/schema/subsonic-rest-api-1.16.1.xsd">
    ///         <seealso cref="http://www.subsonic.org/pages/api.jsp#getIndexes" />
    ///         <seealso cref="https://www.reddit.com/r/subsonic/comments/7c2n6j/database_table_schema/" />
    ///         <!-- Generated the classes from the schema above using 'xsd subsonic-rest-api-1.16.1.xsd /c /f /n:Roadie.Library.Models.Subsonic' from Visual Studio Command Prompt -->
    /// </summary>
    public class SubsonicService : ServiceBase, ISubsonicService
    {
        public const string SubsonicVersion = "1.16.1";

        private IArtistService ArtistService { get; }
        private IBookmarkService BookmarkService { get; }
        private IImageService ImageService { get; }
        private IPlayActivityService PlayActivityService { get; }
        private IPlaylistService PlaylistService { get; }
        private IReleaseService ReleaseService { get; }
        private ITrackService TrackService { get; }
        private UserManager<Library.Identity.User> UserManger { get; }

        public SubsonicService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<SubsonicService> logger,
            IArtistService artistService,
            ITrackService trackService,
            IPlaylistService playlistService,
            IReleaseService releaseService,
            IImageService imageService,
            IBookmarkService bookmarkService,
            IPlayActivityService playActivityService,
            UserManager<Library.Identity.User> userManager
        )
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            ArtistService = artistService;
            BookmarkService = bookmarkService;
            ImageService = imageService;
            PlaylistService = playlistService;
            PlayActivityService = playActivityService;
            ReleaseService = releaseService;
            TrackService = trackService;
            UserManger = userManager;
        }

        /// <summary>
        ///     Adds a message to the chat log.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> AddChatMessageAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.RequiredParameterMissing, "Message is required");
            }

            var chatMessage = new data.ChatMessage
            {
                UserId = roadieUser.Id.Value,
                Message = request.Message
            };
            DbContext.ChatMessages.Add(chatMessage);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok
                }
            };
        }

        /// <summary>
        ///     Authenticate the given credentials and return the corresponding ApplicationUser
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.SubsonicAuthenticateResponse>> AuthenticateAsync(subsonic.Request request)
        {
            if (request == null || string.IsNullOrEmpty(request?.u))
            {
                return new subsonic.SubsonicOperationResult<subsonic.SubsonicAuthenticateResponse>(
                   subsonic.ErrorCodes.WrongUsernameOrPassword, "Unknown Username");
            }

            try
            {
                var user = DbContext.Users.FirstOrDefault(x => x.UserName == request.u);
                if (user == null)
                {
                    Logger.LogTrace($"Unknown User [{request.u}]");
                    return new subsonic.SubsonicOperationResult<subsonic.SubsonicAuthenticateResponse>(
                        subsonic.ErrorCodes.WrongUsernameOrPassword, "Unknown Username");
                }

                var password = request.Password;
                if (!string.IsNullOrEmpty(request.s))
                {
                    try
                    {
                        var token = HashHelper.CreateMD5((user.ApiToken ?? user.Email) + request.s);
                        if (!token.Equals(request.t, StringComparison.OrdinalIgnoreCase))
                        {
                            user = null;
                        }
                    }
                    catch
                    {
                    }
                }

                if (user != null && !string.IsNullOrEmpty(user.PasswordHash) && !string.IsNullOrEmpty(password))
                {
                    try
                    {
                        var hashCheck =
                            UserManger.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                        if (hashCheck == PasswordVerificationResult.Failed)
                        {
                            user = null;
                        }
                    }
                    catch
                    {
                    }
                }

                if (user != null)
                {
                    var now = DateTime.UtcNow;
                    user.LastUpdated = now;
                    user.LastApiAccess = now;
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                }

                if (user == null)
                {
                    Logger.LogTrace($"Invalid Credentials given for User [{request.u}]");
                    return new subsonic.SubsonicOperationResult<subsonic.SubsonicAuthenticateResponse>(
                        subsonic.ErrorCodes.WrongUsernameOrPassword, "Unknown Username");
                }

                Logger.LogTrace($"Subsonic: Successfully Authenticated User [{user}] via Application [{request.c}], Application Version [{request.v}]");
                return new subsonic.SubsonicOperationResult<subsonic.SubsonicAuthenticateResponse>
                {
                    IsSuccess = true,
                    Data = new subsonic.SubsonicAuthenticateResponse
                    {
                        SubsonicUser = user
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"Subsonic.Authenticate, Error CheckPassword [{JsonSerializer.Serialize(request)}]");
            }

            return null;
        }

        /// <summary>
        ///     Creates or updates a bookmark (a position within a media file). Bookmarks are personal and not visible to other
        ///     users.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> CreateBookmarkAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, int position, string comment)
        {
            if (!request.TrackId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Track Id [{request.id}]");
            }

            var track = GetTrack(request.TrackId.Value);
            if (track == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Track Id [{request.TrackId.Value}]");
            }

            var userBookmark = DbContext.Bookmarks.FirstOrDefault(x =>
               x.UserId == roadieUser.Id && x.BookmarkTargetId == track.Id && x.BookmarkType == BookmarkType.Track);
            var createdBookmark = false;
            if (userBookmark == null)
            {
                userBookmark = new data.Bookmark
                {
                    BookmarkTargetId = track.Id,
                    BookmarkType = BookmarkType.Track,
                    UserId = roadieUser.Id.Value,
                    Comment = comment,
                    Position = position
                };
                DbContext.Bookmarks.Add(userBookmark);
                createdBookmark = true;
            }
            else
            {
                userBookmark.LastUpdated = DateTime.UtcNow;
                userBookmark.Position = position;
                userBookmark.Comment = comment;
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            CacheManager.ClearRegion(user.CacheRegion);

            Logger.LogTrace(
                $"{(createdBookmark ? "Created" : "Updated")} Bookmark `{userBookmark}` for User `{roadieUser}`");
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok
                }
            };
        }

        /// <summary>
        ///     Creates (or updates) a playlist.
        /// </summary>
        /// <param name="request">Populated Subsonic Request</param>
        /// <param name="roadieUser">Populated Roadie User</param>
        /// <param name="name">The human-readable name of the playlist.</param>
        /// <param name="songIds">ID of a song in the playlist. Use one songId parameter for each song in the playlist.</param>
        /// <param name="playlistId">The playlist ID. (if updating else blank is adding)</param>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> CreatePlaylistAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, string name, string[] songIds, string playlistId = null)
        {
            data.Playlist playlist = null;

            var songRoadieIds = new Guid?[0];
            var submittedTracks = new data.Track[0].AsQueryable();

            if (songIds?.Any() == true)
            {
                songRoadieIds = songIds.Select(x => SafeParser.ToGuid(x)).ToArray();
                // Add (if not already) given tracks to Playlist
                submittedTracks = from t in DbContext.Tracks
                                  where songRoadieIds.Contains(t.RoadieId)
                                  select t;
            }

            var didCreate = false;
            if (!string.IsNullOrEmpty(playlistId))
            {
                request.id = playlistId;
                playlist = DbContext.Playlists.Include(x => x.Tracks)
                    .FirstOrDefault(x => x.RoadieId == request.PlaylistId);
                if (playlist == null)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                       subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid PlaylistId [{playlistId}]");
                }
                // When Create is called again on an existing delete all existing tracks and add given
                if (playlist.Tracks?.Any() == true)
                {
                    DbContext.PlaylistTracks.RemoveRange(playlist.Tracks);
                }

                var listNumber = playlist.Tracks?.Any() == true
                   ? playlist.Tracks?.Max(x => x.ListNumber) ?? 0
                   : 0;
                foreach (var submittedTrack in submittedTracks)
                {
                    if (playlist.Tracks?.Any(x => x.TrackId == submittedTrack.Id) != true)
                    {
                        listNumber++;
                        DbContext.PlaylistTracks.Add(new data.PlaylistTrack
                        {
                            PlayListId = playlist.Id,
                            ListNumber = listNumber,
                            TrackId = submittedTrack.Id
                        });
                    }
                }

                playlist.Name = name ?? playlist.Name;
                playlist.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var tracks = new List<data.PlaylistTrack>();
                var listNumber = 0;
                foreach (var submittedTrack in submittedTracks)
                {
                    listNumber++;
                    tracks.Add(new data.PlaylistTrack
                    {
                        PlayListId = playlist.Id,
                        ListNumber = listNumber,
                        TrackId = submittedTrack.Id
                    });
                }

                playlist = new data.Playlist
                {
                    IsPublic = false,
                    Name = name,
                    UserId = roadieUser.Id,
                    Tracks = tracks
                };
                didCreate = true;
                DbContext.Playlists.Add(playlist);
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            Logger.LogTrace($"Subsonic: User `{roadieUser}` {(didCreate ? "created" : "modified")} Playlist `{playlist}` added [{songRoadieIds.Length}] Tracks.");
            request.id = $"{subsonic.Request.PlaylistdIdentifier}{playlist.RoadieId}";
            return await GetPlaylistAsync(request, roadieUser).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes the bookmark for a given file.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> DeleteBookmarkAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            if (!request.TrackId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Track Id [{request.id}]");
            }

            var track = GetTrack(request.TrackId.Value);
            if (track == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Track Id [{request.TrackId.Value}]");
            }

            var userBookmark = DbContext.Bookmarks.FirstOrDefault(x =>
               x.UserId == roadieUser.Id && x.BookmarkTargetId == track.Id && x.BookmarkType == BookmarkType.Track);
            if (userBookmark != null)
            {
                DbContext.Bookmarks.Remove(userBookmark);
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
                CacheManager.ClearRegion(user.CacheRegion);

                Logger.LogTrace($"Subsonic: Deleted Bookmark `{userBookmark}` for User `{roadieUser}`");
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok
                }
            };
        }

        /// <summary>
        ///     Deletes a saved playlist.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> DeletePlaylistAsync(subsonic.Request request, Library.Models.Users.User roadieUser)
        {
            //request.PlaylistId.Value

            var deleteResult = await PlaylistService.DeletePlaylistAsync(roadieUser, request.PlaylistId.Value).ConfigureAwait(false);
            if (deleteResult?.IsNotFoundResult != false)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Playlist Id [{request.id}]");
            }
            if (!deleteResult.IsSuccess)
            {
                if (deleteResult.IsAccessDeniedResult)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.UserIsNotAuthorizedForGivenOperation, "User is not allowed to delete playlist.");
                }
                if (deleteResult.Messages?.Any() ?? false)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.Generic, deleteResult.Messages.First());
                }
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.Generic, "An Error Occured");
            }
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok
                }
            };
        }

        /// <summary>
        ///     Returns details for an album, including a list of songs. This method organizes music according to ID3 tags.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetAlbumAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            try
            {
                var releaseId = SafeParser.ToGuid(request.id);
                if (!releaseId.HasValue)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                       subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release [{request.ReleaseId}]");
                }

                var release = await GetRelease(releaseId.Value).ConfigureAwait(false);
                if (release == null)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                       subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release [{request.ReleaseId}]");
                }

                var trackPagedRequest = request.PagedRequest;
                trackPagedRequest.Sort = "TrackNumber";
                trackPagedRequest.Order = "ASC";
                var releaseTracks = await TrackService.ListAsync(trackPagedRequest, roadieUser, false, releaseId).ConfigureAwait(false);
                var userRelease = roadieUser == null
                    ? null
                    : DbContext.UserReleases.FirstOrDefault(x =>
                        x.ReleaseId == release.Id && x.UserId == roadieUser.Id);
                var genre = release.Genres.FirstOrDefault();
                return new subsonic.SubsonicOperationResult<subsonic.Response>
                {
                    IsSuccess = true,
                    Data = new subsonic.Response
                    {
                        version = SubsonicVersion,
                        status = subsonic.ResponseStatus.ok,
                        ItemElementName = subsonic.ItemChoiceType.album,
                        Item = new subsonic.AlbumWithSongsID3
                        {
                            artist = release.Artist.Name,
                            artistId = $"{subsonic.Request.ArtistIdIdentifier}{release.Artist.RoadieId}",
                            coverArt = $"{subsonic.Request.ReleaseIdIdentifier}{release.RoadieId}",
                            created = release.CreatedDate,
                            duration = release.Duration.ToSecondsFromMilliseconds(),
                            genre = (genre?.Genre.Name),
                            id = $"{subsonic.Request.ReleaseIdIdentifier}{release.RoadieId}",
                            name = release.Title,
                            playCount = releaseTracks.Rows.Sum(x => x.PlayedCount) ?? 0,
                            playCountSpecified = releaseTracks.Rows.Any(),
                            songCount = releaseTracks.Rows.Count(),
                            starred = userRelease?.LastUpdated ?? userRelease?.CreatedDate ?? DateTime.UtcNow,
                            starredSpecified = userRelease?.IsFavorite ?? false,
                            year = (release.ReleaseDate?.Year) ?? 0,
                            yearSpecified = release.ReleaseDate != null,
                            song = SubsonicChildrenForTracks(releaseTracks.Rows)
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAlbum Request [{0}], User [{1}]", JsonSerializer.Serialize(request),
                    roadieUser.ToString());
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>(
                subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release [{request.ReleaseId}]");
        }

        /// <summary>
        ///     Returns album notes, image URLs etc, using data from last.fm.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetAlbumInfoAsync(subsonic.Request request, Library.Models.Users.User roadieUser, subsonic.AlbumInfoVersion version)
        {
            var releaseId = SafeParser.ToGuid(request.id);
            if (!releaseId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release [{request.id}]");
            }

            var release = await GetRelease(releaseId.Value).ConfigureAwait(false);
            if (release == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release [{request.id}]");
            }

            switch (version)
            {
                case subsonic.AlbumInfoVersion.One:
                case subsonic.AlbumInfoVersion.Two:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.albumInfo,
                            Item = new subsonic.AlbumInfo
                            {
                                largeImageUrl =
                                    ImageHelper.MakeImage(Configuration, HttpContext, release.RoadieId, "release", Configuration.LargeImageSize).Url,
                                mediumImageUrl = ImageHelper.MakeImage(Configuration, HttpContext, release.RoadieId, "release", Configuration.MediumImageSize)
                                    .Url,
                                smallImageUrl =
                                    ImageHelper.MakeImage(Configuration, HttpContext, release.RoadieId, "release", Configuration.SmallImageSize).Url,
                                lastFmUrl = MakeLastFmUrl(release.Artist.Name, release.Title),
                                musicBrainzId = release.MusicBrainzId,
                                notes = release.Profile
                            }
                        }
                    };

                default:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleServerRestProtocolVersion,
                        $"Unknown Album Info Version [{request.Type}]");
            }
        }

        /// <summary>
        ///     Returns a list of random, newest, highest rated etc. albums. Similar to the album lists on the home page of the
        ///     Subsonic web interface.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetAlbumListAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, subsonic.AlbumListVersions version)
        {
            var releaseResult = new PagedResult<ReleaseList>();

            switch (request.Type)
            {
                case subsonic.ListType.Random:
                    releaseResult = await ReleaseService.ListAsync(roadieUser, request.PagedRequest, true).ConfigureAwait(false);
                    break;

                case subsonic.ListType.Highest:
                case subsonic.ListType.Recent:
                case subsonic.ListType.Newest:
                case subsonic.ListType.Frequent:
                case subsonic.ListType.AlphabeticalByName:
                case subsonic.ListType.AlphabeticalByArtist:
                case subsonic.ListType.Starred:
                case subsonic.ListType.ByGenre:
                case subsonic.ListType.ByYear:
                    releaseResult = await ReleaseService.ListAsync(roadieUser, request.PagedRequest).ConfigureAwait(false);
                    break;

                default:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleServerRestProtocolVersion,
                        $"Unknown Album List Type [{request.Type}]");
            }

            if (!releaseResult.IsSuccess)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(releaseResult.Message);
            }

            switch (version)
            {
                case subsonic.AlbumListVersions.One:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.albumList,
                            Item = new subsonic.AlbumList
                            {
                                album = SubsonicChildrenForReleases(releaseResult.Rows, null)
                            }
                        }
                    };

                case subsonic.AlbumListVersions.Two:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.albumList2,
                            Item = new subsonic.AlbumList2
                            {
                                album = SubsonicAlbumID3ForReleases(releaseResult.Rows)
                            }
                        }
                    };

                default:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleServerRestProtocolVersion,
                        $"Unknown AlbumListVersions [{version}]");
            }
        }

        /// <summary>
        ///     Returns details for an artist, including a list of albums. This method organizes music according to ID3 tags.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetArtistAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            var artistId = SafeParser.ToGuid(request.id);
            if (!artistId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release [{request.id}]");
            }

            var pagedRequest = request.PagedRequest;
            pagedRequest.Sort = "Id";
            pagedRequest.FilterToArtistId = artistId.Value;
            var artistResult = await ArtistService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
            var artist = artistResult.Rows.FirstOrDefault();
            if (artist == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release [{request.id}]");
            }

            var artistReleaseResult = await ReleaseService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.artist,
                    Item = SubsonicArtistWithAlbumsID3ForArtist(artist, SubsonicAlbumID3ForReleases(artistReleaseResult.Rows))
                }
            };
        }

        /// <summary>
        ///     Returns artist info with biography, image URLs and similar artists, using data from last.fm.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetArtistInfoAsync(subsonic.Request request, int? count, bool includeNotPresent, subsonic.ArtistInfoVersion version)
        {
            var artistId = SafeParser.ToGuid(request.id);
            if (!artistId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid ArtistId [{request.id}]");
            }

            var artist = await GetArtist(artistId.Value).ConfigureAwait(false);
            if (artist == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid ArtistId [{request.id}]");
            }
            switch (version)
            {
                case subsonic.ArtistInfoVersion.One:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.artistInfo,
                            Item = SubsonicArtistInfoForArtist(artist)
                        }
                    };

                case subsonic.ArtistInfoVersion.Two:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.artistInfo2,
                            Item = SubsonicArtistInfo2InfoForArtist(artist)
                        }
                    };

                default:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleServerRestProtocolVersion,
                        $"Unknown ArtistInfoVersion [{version}]");
            }
        }

        /// <summary>
        ///     Similar to getIndexes, but organizes music according to ID3 tags.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetArtistsAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            var cacheKey = $"urn:subsonic_artists:{roadieUser.UserName}";
            return await CacheManager.GetAsync(cacheKey,
                async () => await GetArtistsAction(request, roadieUser).ConfigureAwait(false),
                CacheManagerBase.SystemCacheRegionUrn).ConfigureAwait(false);
        }

        /// <summary>
        ///     Returns all bookmarks for this user. A bookmark is a position within a certain media file.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetBookmarksAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            var pagedRequest = request.PagedRequest;
            pagedRequest.Sort = "LastUpdated";
            pagedRequest.Order = "DESC";
            var userBookmarkResult = await BookmarkService.ListAsync(roadieUser, pagedRequest, false, BookmarkType.Track).ConfigureAwait(false);
            pagedRequest.FilterToTrackIds =
                userBookmarkResult.Rows.Select(x => SafeParser.ToGuid(x.Bookmark.Value)).ToArray();
            var trackListResult = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.bookmarks,
                    Item = new subsonic.Bookmarks
                    {
                        bookmark = SubsonicBookmarksForBookmarks(userBookmarkResult.Rows, trackListResult.Rows)
                    }
                }
            };
        }

        /// <summary>
        ///     Returns the current visible (non-expired) chat messages.
        /// </summary>
        public Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetChatMessagesAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, long? since)
        {
            var messagesSince = since?.FromUnixTime();
            var chatMessages = (from cm in DbContext.ChatMessages
                                join u in DbContext.Users on cm.UserId equals u.Id
                                where messagesSince == null || cm.CreatedDate >= messagesSince
                                where cm.Status != Statuses.Deleted
                                orderby cm.CreatedDate descending
                                select new subsonic.ChatMessage
                                {
                                    message = cm.Message,
                                    username = u.UserName,
                                    time = cm.CreatedDate.ToUnixTime()
                                }).ToArray();
            return Task.FromResult(new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.chatMessages,
                    Item = new subsonic.ChatMessages
                    {
                        chatMessage = chatMessages.ToArray()
                    }
                }
            });
        }

        /// <summary>
        ///     Returns a cover art image.
        /// </summary>
        public async Task<subsonic.SubsonicFileOperationResult<Library.Models.Image>> GetCoverArtAsync(subsonic.Request request, int? size)
        {
            var sw = Stopwatch.StartNew();
            var result = new subsonic.SubsonicFileOperationResult<Library.Models.Image>();
            if (request.ArtistId != null)
            {
                var artistImage = await ImageService.ArtistImageAsync(request.ArtistId.Value, size, size).ConfigureAwait(false);
                if (!artistImage.IsSuccess)
                {
                    return artistImage.Adapt<subsonic.SubsonicFileOperationResult<Library.Models.Image>>();
                }

                result.Data = new Library.Models.Image(artistImage.Data.Bytes);
            }
            else if (request.TrackId != null)
            {
                var trackimage = await ImageService.TrackImageAsync(request.TrackId.Value, size, size).ConfigureAwait(false);
                if (!trackimage.IsSuccess)
                {
                    return trackimage.Adapt<subsonic.SubsonicFileOperationResult<Library.Models.Image>>();
                }

                result.Data = new Library.Models.Image(trackimage.Data.Bytes);
            }
            else if (request.CollectionId != null)
            {
                var collectionImage = await ImageService.CollectionImageAsync(request.CollectionId.Value, size, size).ConfigureAwait(false);
                if (!collectionImage.IsSuccess)
                {
                    return collectionImage.Adapt<subsonic.SubsonicFileOperationResult<Library.Models.Image>>();
                }

                result.Data = new Library.Models.Image(collectionImage.Data.Bytes);
            }
            else if (request.ReleaseId != null)
            {
                var releaseimage = await ImageService.ReleaseImageAsync(request.ReleaseId.Value, size, size).ConfigureAwait(false);
                if (!releaseimage.IsSuccess)
                {
                    return releaseimage.Adapt<subsonic.SubsonicFileOperationResult<Library.Models.Image>>();
                }

                result.Data = new Library.Models.Image(releaseimage.Data.Bytes);
            }
            else if (request.PlaylistId != null)
            {
                var playlistImage = await ImageService.PlaylistImageAsync(request.PlaylistId.Value, size, size).ConfigureAwait(false);
                if (!playlistImage.IsSuccess)
                {
                    return playlistImage.Adapt<subsonic.SubsonicFileOperationResult<Library.Models.Image>>();
                }

                result.Data = new Library.Models.Image(playlistImage.Data.Bytes);
            }
            else if (!string.IsNullOrEmpty(request.u))
            {
                var user = await GetUser(request.u).ConfigureAwait(false);
                if (user == null)
                {
                    return new subsonic.SubsonicFileOperationResult<Library.Models.Image>(
                       subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Username [{request.u}]");
                }

                var userImage = await ImageService.UserImageAsync(user.RoadieId, size, size).ConfigureAwait(false);
                if (!userImage.IsSuccess)
                {
                    return userImage.Adapt<subsonic.SubsonicFileOperationResult<Library.Models.Image>>();
                }

                result.Data = new Library.Models.Image(userImage.Data.Bytes);
            }

            result.IsSuccess = result.Data.Bytes != null;
            sw.Stop();
            return new subsonic.SubsonicFileOperationResult<Library.Models.Image>(result.Messages)
            {
                Data = result.Data,
                ETag = result.ETag,
                LastModified = result.LastModified,
                ContentType = "image/jpeg",
                Errors = result?.Errors,
                IsSuccess = result.IsSuccess,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        /// <summary>
        ///     Returns all genres
        /// </summary>
        public Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetGenresAsync(subsonic.Request request)
        {
            var genres = (from g in DbContext.Genres
                          let albumCount = (from rg in DbContext.ReleaseGenres
                                            where rg.GenreId == g.Id
                                            select rg.Id).Count()
                          let songCount = (from rg in DbContext.ReleaseGenres
                                           join rm in DbContext.ReleaseMedias on rg.ReleaseId equals rm.ReleaseId
                                           join t in DbContext.Tracks on rm.ReleaseId equals t.ReleaseMediaId
                                           where rg.GenreId == g.Id
                                           select t.Id).Count()
                          select new subsonic.Genre
                          {
                              songCount = songCount,
                              albumCount = albumCount,
                              value = g.Name
                          }).OrderBy(x => x.value).ToArray();

            return Task.FromResult(new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.genres,
                    Item = new subsonic.Genres
                    {
                        genre = genres.ToArray()
                    }
                }
            });
        }

        /// <summary>
        ///     Returns an indexed structure of all artists.
        /// </summary>
        /// <param name="request">Query from application.</param>
        /// <param name="ifModifiedSince">
        ///     If specified, only return a result if the artist collection has changed since the given
        ///     time (in milliseconds since 1 Jan 1970).
        /// </param>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetIndexesAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, long? ifModifiedSince = null)
        {
            const string cacheKey = "urn:subsonic_indexes";
            return await CacheManager.GetAsync(cacheKey, async () =>
            {
                // Dont send the user to get index list as user data (likes, dislikes, etc.) aren't used in this list and dont need performance hit
                return await GetIndexesAction(request, null, ifModifiedSince).ConfigureAwait(false);
            }, CacheManagerBase.SystemCacheRegionUrn).ConfigureAwait(false);
        }

        /// <summary>
        ///     Get details about the software license. Takes no extra parameters. Roadies gives everyone a premium 1 year license
        ///     everytime they ask :)
        /// </summary>
        public subsonic.SubsonicOperationResult<subsonic.Response> GetLicense(subsonic.Request request)
        {
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.license,
                    Item = new subsonic.License
                    {
                        email = Configuration.SmtpFromAddress,
                        valid = true,
                        licenseExpires = DateTime.UtcNow.AddYears(1),
                        licenseExpiresSpecified = true
                    }
                }
            };
        }

        /// <summary>
        ///     Searches for and returns lyrics for a given song
        /// </summary>
        public subsonic.SubsonicOperationResult<subsonic.Response> GetLyrics(subsonic.Request request, string artistId,
            string title)
        {
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.lyrics,
                    Item = new subsonic.Lyrics
                    {
                        artist = artistId,
                        title = title,
                        Text = new string[0]
                    }
                }
            };
        }

        /// <summary>
        ///     Returns a listing of all files in a music directory. Typically used to get list of albums for an artist, or list of
        ///     songs for an album.
        /// </summary>
        /// <param name="request">Query from application.</param>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetMusicDirectoryAsync(subsonic.Request request, Library.Models.Users.User roadieUser)
        {
            var directory = new subsonic.Directory();
            var user = await GetUser(roadieUser?.UserId).ConfigureAwait(false);

            // Request to get albums for an Artist
            if (request.ArtistId != null)
            {
                var artistId = SafeParser.ToGuid(request.id);
                var artist = await GetArtist(artistId.Value).ConfigureAwait(false);
                if (artist == null)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                       subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid ArtistId [{request.id}]");
                }

                directory.id = $"{subsonic.Request.ArtistIdIdentifier}{artist.RoadieId}";
                directory.name = artist.Name;
                var artistRating = user == null
                    ? null
                    : DbContext.UserArtists.FirstOrDefault(x => x.UserId == user.Id && x.ArtistId == artist.Id);
                if (artistRating?.IsFavorite ?? false)
                {
                    directory.starred = artistRating.LastUpdated ?? artistRating.CreatedDate;
                    directory.starredSpecified = true;
                }

                var pagedRequest = request.PagedRequest;
                pagedRequest.FilterToArtistId = artist.RoadieId;
                pagedRequest.Sort = "Release.Text";
                var artistReleases = await ReleaseService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
                directory.child = SubsonicChildrenForReleases(artistReleases.Rows,
                    $"{subsonic.Request.ArtistIdIdentifier}{artist.RoadieId}");
            }
            // Request to get albums for in a Collection
            else if (request.CollectionId != null)
            {
                var collectionId = SafeParser.ToGuid(request.id);
                var collection = await GetCollection(collectionId.Value).ConfigureAwait(false);
                if (collection == null)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                       subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid CollectionId [{request.id}]");
                }

                directory.id = $"{subsonic.Request.CollectionIdentifier}{collection.RoadieId}";
                directory.name = collection.Name;
                var pagedRequest = request.PagedRequest;
                pagedRequest.FilterToCollectionId = collection.RoadieId;
                var collectionReleases = await ReleaseService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
                directory.child = SubsonicChildrenForReleases(collectionReleases.Rows,
                    $"{subsonic.Request.CollectionIdentifier}{collection.RoadieId}");
            }
            // Request to get Tracks for an Album
            else if (request.ReleaseId.HasValue)
            {
                var releaseId = SafeParser.ToGuid(request.id);
                var release = await GetRelease(releaseId.Value).ConfigureAwait(false);
                if (release == null)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                       subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid ReleaseId [{request.id}]");
                }

                directory.id = $"{subsonic.Request.ReleaseIdIdentifier}{release.RoadieId}";
                directory.name = release.Title;
                var releaseRating = user == null
                    ? null
                    : await DbContext.UserReleases.FirstOrDefaultAsync(x => x.UserId == user.Id && x.ReleaseId == release.Id).ConfigureAwait(false);
                directory.averageRating = release.Rating ?? 0;
                directory.parent = $"{subsonic.Request.ArtistIdIdentifier}{release.Artist.RoadieId}";
                if (releaseRating?.IsFavorite ?? false)
                {
                    directory.starred = releaseRating.LastUpdated ?? releaseRating.CreatedDate;
                    directory.starredSpecified = true;
                }

                var trackPagedRequest = request.PagedRequest;
                trackPagedRequest.Sort = "TrackNumber";
                trackPagedRequest.Order = "ASC";
                var songTracks = await TrackService.ListAsync(trackPagedRequest, roadieUser, false, release.RoadieId).ConfigureAwait(false);
                directory.child = SubsonicChildrenForTracks(songTracks.Rows);
                directory.playCount = directory.child.Select(x => x.playCount).Sum();
            }
            else
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                    subsonic.ErrorCodes.TheRequestedDataWasNotFound,
                    $"Unknown GetMusicDirectory Type [{JsonSerializer.Serialize(request)}], id [{request.id}]");
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.directory,
                    Item = directory
                }
            };
        }

        /// <summary>
        ///     Returns all configured top-level music folders. Takes no extra parameters.
        /// </summary>
        public Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetMusicFoldersAsync(subsonic.Request request)
        {
            return Task.FromResult(new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.musicFolders,
                    Item = new subsonic.MusicFolders
                    {
                        musicFolder = MusicFolders().ToArray()
                    }
                }
            });
        }

        /// <summary>
        ///     Returns what is currently being played by all users. Takes no extra parameters.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetNowPlayingAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            var pagedRequest = request.PagedRequest;
            pagedRequest.Sort = "PlayedDateDateTime";
            pagedRequest.Order = "DESC";
            var playActivityResult =
                await PlayActivityService.ListAsync(pagedRequest, roadieUser, DateTime.UtcNow.AddDays(-1)).ConfigureAwait(false);

            pagedRequest.Sort = null;
            pagedRequest.Order = null;
            pagedRequest.FilterToTrackIds = playActivityResult.Rows.Select(x => SafeParser.ToGuid(x.Track.Track.Value))
                .Distinct().ToArray();
            var playActivityTracksResult = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);

            var playEntries = new List<subsonic.NowPlayingEntry>();
            var now = DateTime.UtcNow;
            foreach (var row in playActivityResult.Rows)
            {
                var rowTrack =
                    playActivityTracksResult.Rows.FirstOrDefault(x => x.Track.Value == row.Track.Track.Value);
                var playEntryTrackChild = SubsonicChildForTrack(rowTrack);
                var playEntry = playEntryTrackChild.Adapt<subsonic.NowPlayingEntry>();
                playEntry.username = row.User.Text;
                playEntry.minutesAgo = (int)(now - row.PlayedDateDateTime.Value).TotalMinutes;
                playEntry.playerId = 0;
                playEntry.playerName = string.Empty;
                playEntries.Add(playEntry);
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.nowPlaying,
                    Item = new subsonic.NowPlaying
                    {
                        entry = playEntries.ToArray()
                    }
                }
            };
        }

        /// <summary>
        ///     Returns a listing of files in a saved playlist.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetPlaylistAsync(subsonic.Request request, Library.Models.Users.User roadieUser)
        {
            var playListId = SafeParser.ToGuid(request.id);
            if (!playListId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid PlaylistId [{request.id}]");
            }
            var pagedRequest = request.PagedRequest;
            pagedRequest.Sort = "Id";
            pagedRequest.FilterToPlaylistId = playListId.Value;
            var playlistResult = await PlaylistService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);
            var playlist = playlistResult.Rows.FirstOrDefault();
            if (playlist == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid PlaylistId [{request.id}]");
            }
            // For a playlist to show all the tracks in the playlist set the limit to the playlist size
            pagedRequest.Limit = playlist.PlaylistCount ?? pagedRequest.Limit;
            var tracksForPlaylist = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);
            var detail = await SubsonicPlaylistForPlaylist(playlist, tracksForPlaylist.Rows).ConfigureAwait(false);
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.playlist,
                    Item = detail
                }
            };
        }

        /// <summary>
        ///     Returns all playlists a user is allowed to play.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetPlaylistsAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, string filterToUserName)
        {
            var pagedRequest = request.PagedRequest;
            pagedRequest.Sort = "Playlist.Text";
            pagedRequest.Order = "ASC";
            var playlistResult = await PlaylistService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.playlists,
                    Item = new subsonic.Playlists
                    {
                        playlist = await SubsonicPlaylistsForPlaylists(playlistResult.Rows).ConfigureAwait(false)
                    }
                }
            };
        }

        /// <summary>
        ///     Returns the state of the play queue for this user (as set by savePlayQueue). This includes the tracks in the play
        ///     queue, the currently playing track, and the position within this track. Typically used to allow a user to move
        ///     between different clients/apps while retaining the same play queue (for instance when listening to an audio book).
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetPlayQueueAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);

            subsonic.PlayQueue playQue = null;

            if (user.UserQues?.Any() == true)
            {
                var current = user.UserQues.FirstOrDefault(x => x.IsCurrent ?? false) ?? user.UserQues.First();
                var pagedRequest = request.PagedRequest;
                pagedRequest.FilterToTrackIds = user.UserQues.Select(x => x.Track?.RoadieId).ToArray();
                var queTracksResult = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);
                var queTrackRows = (from tt in queTracksResult.Rows
                                    join qt in user.UserQues on tt.DatabaseId equals qt.TrackId
                                    orderby qt.QueSortOrder
                                    select tt);
                playQue = new subsonic.PlayQueue
                {
                    // I didnt specify current as it appears to be a Int and it blows up several client applications changing it to a string.
                    // current = subsonic.Request.TrackIdIdentifier + current.Track.RoadieId.ToString(),
                    changedBy = user.UserName,
                    changed = user.UserQues.OrderByDescending(x => x.CreatedDate).First().CreatedDate,
                    position = current.Position ?? 0,
                    positionSpecified = current.Position.HasValue,
                    username = user.UserName,
                    entry = SubsonicChildrenForTracks(queTrackRows)
                };
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                IsEmptyResponse = playQue == null,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.playQueue,
                    Item = playQue
                }
            };
        }

        /// <summary>
        ///     Returns all Podcast channels the server subscribes to, and (optionally) their episodes. This method can also be
        ///     used to return details for only one channel - refer to the id parameter. A typical use case for this method would
        ///     be to first retrieve all channels without episodes, and then retrieve all episodes for the single channel the user
        ///     selects.
        /// </summary>
        public Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetPodcastsAsync(subsonic.Request request)
        {
            return Task.FromResult(new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.podcasts,
                    Item = new subsonic.Podcasts
                    {
                        channel = new subsonic.PodcastChannel[0]
                    }
                }
            });
        }

        /// <summary>
        ///     Returns random songs matching the given criteria.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetRandomSongsAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            var songs = new List<subsonic.Child>();

            var randomSongs = await TrackService.ListAsync(request.PagedRequest, roadieUser, true).ConfigureAwait(false);

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.randomSongs,
                    Item = new subsonic.Songs
                    {
                        song = SubsonicChildrenForTracks(randomSongs.Rows)
                    }
                }
            };
        }

        /// <summary>
        ///     Returns a random collection of songs from the given artist and similar artists, using data from last.fm. Typically
        ///     used for artist radio features.
        /// </summary>
        public Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetSimliarSongsAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, subsonic.SimilarSongsVersion version, int? count = 50)
        {
            // TODO How to determine similar songs? Perhaps by genre?

            switch (version)
            {
                case subsonic.SimilarSongsVersion.One:
                    return Task.FromResult(new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.similarSongs,
                            Item = new subsonic.SimilarSongs
                            {
                                song = new subsonic.Child[0]
                            }
                        }
                    });

                case subsonic.SimilarSongsVersion.Two:
                    return Task.FromResult(new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.similarSongs2,
                            Item = new subsonic.SimilarSongs2
                            {
                                song = new subsonic.Child[0]
                            }
                        }
                    });

                default:
                    return Task.FromResult(new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleServerRestProtocolVersion,
                        $"Unknown SimilarSongsVersion [{version}]"));
            }
        }

        /// <summary>
        ///     Returns details for a song.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetSongAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            if (!request.TrackId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Track [{request.id}]");
            }

            var pagedRequest = request.PagedRequest;
            pagedRequest.FilterToTrackId = request.TrackId.Value;
            pagedRequest.Sort = "Id";
            var trackResult = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);
            var track = trackResult.Rows.FirstOrDefault();
            if (track == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Track [{request.id}]");
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.song,
                    Item = SubsonicChildForTrack(track)
                }
            };
        }

        /// <summary>
        ///     Returns songs in a given genre.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetSongsByGenreAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser)
        {
            var pagedRequest = request.PagedRequest;
            pagedRequest.FilterByGenre = request.Genre;
            pagedRequest.Sort = "Id";
            var trackResult = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.songsByGenre,
                    Item = new subsonic.Songs
                    {
                        song = SubsonicChildrenForTracks(trackResult.Rows)
                    }
                }
            };
        }

        /// <summary>
        ///     Returns starred songs, albums and artists.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetStarredAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, subsonic.StarredVersion version)
        {
            var pagedRequest = request.PagedRequest;
            pagedRequest.FilterFavoriteOnly = true;
            pagedRequest.Sort = "Id";

            var artistList = await ArtistService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
            var releaseList = await ReleaseService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
            var songList = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);

            switch (version)
            {
                case subsonic.StarredVersion.One:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.starred,
                            Item = new subsonic.Starred
                            {
                                album = SubsonicChildrenForReleases(releaseList.Rows, null),
                                artist = SubsonicArtistsForArtists(artistList.Rows),
                                song = SubsonicChildrenForTracks(songList.Rows)
                            }
                        }
                    };

                case subsonic.StarredVersion.Two:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.starred2,
                            Item = new subsonic.Starred2
                            {
                                album = SubsonicAlbumID3ForReleases(releaseList.Rows),
                                artist = SubsonicArtistID3sForArtists(artistList.Rows),
                                song = SubsonicChildrenForTracks(songList.Rows)
                            }
                        }
                    };

                default:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleServerRestProtocolVersion,
                        $"Unknown StarredVersion [{version}]");
            }
        }

        /// <summary>
        ///     Returns top songs for the given artist, using data from last.fm.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetTopSongsAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, int? count = 50)
        {
            data.Artist artist = null;
            if (!string.IsNullOrEmpty(request.ArtistName))
            {
                artist = await GetArtist(request.ArtistName).ConfigureAwait(false);
            }
            else if (request.ArtistId.HasValue)
            {
                artist = await GetArtist(request.ArtistId.Value).ConfigureAwait(false);
            }

            if (artist == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Unknown Artist [{request.ArtistName}]");
            }

            var pagedRequest = request.PagedRequest;
            pagedRequest.FilterToArtistId = artist.RoadieId;
            pagedRequest.FilterTopPlayedOnly = true;
            pagedRequest.Sort = "PlayedCount";
            pagedRequest.Order = "DESC";
            var trackResult = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.topSongs,
                    Item = new subsonic.TopSongs
                    {
                        song = SubsonicChildrenForTracks(trackResult.Rows)
                    }
                }
            };
        }

        /// <summary>
        ///     Get details about a given user, including which authorization roles and folder access it has. Can be used to
        ///     enable/disable certain features in the client, such as jukebox control.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetUserAsync(subsonic.Request request,
            string username)
        {
            var user = await GetUser(username).ConfigureAwait(false);
            if (user == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Username [{username}]");
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.user,
                    Item = await SubsonicUserForUser(user).ConfigureAwait(false)
                }
            };
        }

        /// <summary>
        ///     Returns all video files.
        /// </summary>
        public subsonic.SubsonicOperationResult<subsonic.Response> GetVideos(subsonic.Request request)
        {
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.videos,
                    Item = new subsonic.Videos
                    {
                        video = new subsonic.Child[0]
                    }
                }
            };
        }

        /// <summary>
        ///     Used to test connectivity with the server. Takes no extra parameters.
        /// </summary>
        public subsonic.SubsonicOperationResult<subsonic.Response> Ping(subsonic.Request request)
        {
            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok
                }
            };
        }

        /// <summary>
        ///     Saves the state of the play queue for this user. This includes the tracks in the play queue, the currently playing
        ///     track, and the position within this track. Typically used to allow a user to move between different clients/apps
        ///     while retaining the same play queue (for instance when listening to an audio book).
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> SavePlayQueueAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, string current, long? position)
        {
            // Remove any existing Que for User
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user.UserQues?.Any() == true)
            {
                DbContext.UserQues.RemoveRange(user.UserQues);
            }
            // Create a new UserQue for each posted TrackId in ids
            if (request.ids?.Any() == true)
            {
                short queSortOrder = 0;
                var pagedRequest = request.PagedRequest;
                pagedRequest.FilterToTrackIds =
                    request.ids.Select(x => SafeParser.ToGuid(x)).Where(x => x.HasValue).ToArray();
                var trackListResult = await TrackService.ListAsync(pagedRequest, roadieUser).ConfigureAwait(false);
                var currentTrackId = SafeParser.ToGuid(current);
                foreach (var row in trackListResult.Rows)
                {
                    queSortOrder++;
                    DbContext.UserQues.Add(new data.UserQue
                    {
                        IsCurrent = row.Track.Value == currentTrackId?.ToString(),
                        Position = row.Track.Value == currentTrackId?.ToString() ? position : null,
                        QueSortOrder = queSortOrder,
                        TrackId = row.DatabaseId,
                        UserId = user.Id
                    });
                }
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            CacheManager.ClearRegion(user.CacheRegion);

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok
                }
            };
        }

        /// <summary>
        ///     Returns albums, artists and songs matching the given search criteria. Supports paging through the result.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> SearchAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, subsonic.SearchVersion version)
        {
            var query = HttpEncoder.UrlDecode(request.Query).Replace("*", string.Empty).Replace("%", string.Empty).Replace(";", string.Empty);

            // Search artists with query returning ArtistCount skipping ArtistOffset
            var artistPagedRequest = request.PagedRequest;
            artistPagedRequest.Sort = "Artist.Text";
            artistPagedRequest.Limit = request.ArtistCount ?? artistPagedRequest.Limit;
            artistPagedRequest.SkipValue = request.ArtistOffset ?? artistPagedRequest.SkipValue;
            artistPagedRequest.Filter = query;
            var artistResult = await ArtistService.ListAsync(roadieUser, artistPagedRequest).ConfigureAwait(false);

            // Search release with query returning RelaseCount skipping ReleaseOffset
            var releasePagedRequest = request.PagedRequest;
            releasePagedRequest.Sort = "Release.Text";
            releasePagedRequest.Limit = request.AlbumCount ?? releasePagedRequest.Limit;
            releasePagedRequest.SkipValue = request.AlbumOffset ?? releasePagedRequest.SkipValue;
            releasePagedRequest.Filter = query;
            var releaseResult = await ReleaseService.ListAsync(roadieUser, releasePagedRequest).ConfigureAwait(false);

            // Search tracks with query returning SongCount skipping SongOffset
            var trackPagedRequest = request.PagedRequest;
            trackPagedRequest.Sort = "Track.Text";
            trackPagedRequest.Limit = request.SongCount ?? trackPagedRequest.Limit;
            trackPagedRequest.SkipValue = request.SongOffset ?? trackPagedRequest.SkipValue;
            trackPagedRequest.Filter = query;
            var songResult = await TrackService.ListAsync(trackPagedRequest, roadieUser).ConfigureAwait(false);
            var songs = SubsonicChildrenForTracks(songResult.Rows);

            switch (version)
            {
                case subsonic.SearchVersion.One:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleClientRestProtocolVersion,
                        "Deprecated since 1.4.0, use search2 instead.");

                case subsonic.SearchVersion.Two:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.searchResult2,
                            Item = new subsonic.SearchResult2
                            {
                                artist = SubsonicArtistsForArtists(artistResult.Rows),
                                album = SubsonicChildrenForReleases(releaseResult.Rows, null),
                                song = songs.ToArray()
                            }
                        }
                    };

                case subsonic.SearchVersion.Three:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response
                        {
                            version = SubsonicVersion,
                            status = subsonic.ResponseStatus.ok,
                            ItemElementName = subsonic.ItemChoiceType.searchResult3,
                            Item = new subsonic.SearchResult3
                            {
                                artist = SubsonicArtistID3sForArtists(artistResult.Rows),
                                album = SubsonicAlbumID3ForReleases(releaseResult.Rows),
                                song = songs.ToArray()
                            }
                        }
                    };

                default:
                    return new subsonic.SubsonicOperationResult<subsonic.Response>(
                        subsonic.ErrorCodes.IncompatibleServerRestProtocolVersion,
                        $"Unknown SearchVersion [{version}]");
            }
        }

        /// <summary>
        ///     Sets the rating for a music file. If rating is zero then remove rating.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> SetRatingAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, short rating)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.UserIsNotAuthorizedForGivenOperation, $"Invalid User [{roadieUser}]");
            }

            // Id can be a song, album or artist
            if (request.TrackId.HasValue)
            {
                var starResult = await SetTrackRating(request.TrackId.Value, user, rating).ConfigureAwait(false);
                if (starResult.IsSuccess)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response()
                    };
                }
            }
            else if (request.ReleaseId.HasValue)
            {
                var starResult = await SetReleaseRating(request.ReleaseId.Value, user, rating).ConfigureAwait(false);
                if (starResult.IsSuccess)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response()
                    };
                }
            }
            else if (request.ArtistId.HasValue)
            {
                var starResult = await SetArtistRating(request.ArtistId.Value, user, rating).ConfigureAwait(false);
                if (starResult.IsSuccess)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response()
                    };
                }
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>(
                subsonic.ErrorCodes.TheRequestedDataWasNotFound,
                $"Unknown Star Id [{JsonSerializer.Serialize(request)}]");
        }

        /// <summary>
        ///     Attaches a star to a song, album or artist.
        /// </summary>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> ToggleStarAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, bool star, string[] albumIds = null, string[] artistIds = null)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.UserIsNotAuthorizedForGivenOperation, $"Invalid User [{roadieUser}]");
            }

            // Id can be a song, album or artist
            if (request.TrackId.HasValue)
            {
                var starResult = await ToggleTrackStar(request.TrackId.Value, user, star).ConfigureAwait(false);
                if (starResult.IsSuccess)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response()
                    };
                }
            }
            else if (request.ReleaseId.HasValue)
            {
                var starResult = await ToggleReleaseStar(request.ReleaseId.Value, user, star).ConfigureAwait(false);
                if (starResult.IsSuccess)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response()
                    };
                }
            }
            else if (request.ArtistId.HasValue)
            {
                var starResult = await ToggleArtistStar(request.ArtistId.Value, user, star).ConfigureAwait(false);
                if (starResult.IsSuccess)
                {
                    return new subsonic.SubsonicOperationResult<subsonic.Response>
                    {
                        IsSuccess = true,
                        Data = new subsonic.Response()
                    };
                }
            }
            else if (albumIds?.Any() == true)
            {
                foreach (var rId in albumIds)
                {
                    var releaseId = SafeParser.ToGuid(rId);
                    if (releaseId.HasValue)
                    {
                        var starResult = await ToggleReleaseStar(releaseId.Value, user, star).ConfigureAwait(false);
                        if (!starResult.IsSuccess)
                        {
                            return new subsonic.SubsonicOperationResult<subsonic.Response>(starResult.ErrorCode.Value,
                               starResult.Messages.FirstOrDefault());
                        }
                    }
                }
            }
            else if (artistIds?.Any() == true)
            {
                foreach (var aId in artistIds)
                {
                    var artistId = SafeParser.ToGuid(aId);
                    if (artistId.HasValue)
                    {
                        var starResult = await ToggleReleaseStar(artistId.Value, user, star).ConfigureAwait(false);
                        if (!starResult.IsNotFoundResult)
                        {
                            return new subsonic.SubsonicOperationResult<subsonic.Response>(starResult.ErrorCode.Value,
                               starResult.Messages.FirstOrDefault());
                        }
                    }
                }
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>(
                subsonic.ErrorCodes.TheRequestedDataWasNotFound,
                $"Unknown Star Id [{JsonSerializer.Serialize(request)}]");
        }

        /// <summary>
        ///     Updates a playlist. Only the owner of a playlist is allowed to update it.
        /// </summary>
        /// <param name="request">Populated Subsonic Request</param>
        /// <param name="roadieUser">Populated Roadie User</param>
        /// <param name="name">The human-readable name of the playlist.</param>
        /// <param name="comment">The playlist comment.</param>
        /// <param name="isPublic">true if the playlist should be visible to all users, false otherwise.</param>
        /// <param name="songIdsToAdd">Add this song with this ID to the playlist. Multiple parameters allowed</param>
        /// <param name="songIndexesToRemove">Remove the song at this position in the playlist. Multiple parameters allowed.</param>
        public async Task<subsonic.SubsonicOperationResult<subsonic.Response>> UpdatePlaylistAsync(subsonic.Request request,
            Library.Models.Users.User roadieUser, string playListId, string name = null, string comment = null, bool? isPublic = null,
            string[] songIdsToAdd = null, int[] songIndexesToRemove = null)
        {
            request.id = playListId ?? request.id;
            if (!request.PlaylistId.HasValue)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Playlist Id [{request.id}]");
            }

            var playlist = await GetPlaylist(request.PlaylistId.Value).ConfigureAwait(false);
            if (playlist == null)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Playlist Id [{request.TrackId.Value}]");
            }

            if (playlist.UserId != roadieUser.Id && !roadieUser.IsAdmin)
            {
                return new subsonic.SubsonicOperationResult<subsonic.Response>(
                   subsonic.ErrorCodes.UserIsNotAuthorizedForGivenOperation,
                   "User is not allowed to update playlist.");
            }

            playlist.Name = name ?? playlist.Name;
            playlist.IsPublic = isPublic ?? playlist.IsPublic;
            playlist.LastUpdated = DateTime.UtcNow;

            if (songIdsToAdd?.Any() == true)
            {
                // Add new if not already on Playlist
                var songIdsToAddRoadieIds = songIdsToAdd.Select(x => SafeParser.ToGuid(x)).ToArray();
                var submittedTracks = from t in DbContext.Tracks
                                      where songIdsToAddRoadieIds.Contains(t.RoadieId)
                                      select t;

                var listNumber = playlist.Tracks?.Max(x => x.ListNumber) ?? 0;
                foreach (var submittedTrack in submittedTracks)
                {
                    if (playlist.Tracks == null || playlist.Tracks?.Any(x => x.TrackId == submittedTrack.Id) != true)
                    {
                        listNumber++;
                        DbContext.PlaylistTracks.Add(new data.PlaylistTrack
                        {
                            PlayListId = playlist.Id,
                            ListNumber = listNumber,
                            TrackId = submittedTrack.Id
                        });
                    }
                }
            }

            if (songIndexesToRemove?.Any() == true)
            {
                // Remove tracks from playlist
                // Not clear from API documentation if this is zero based, wait until someone calls it to get values passed.
                throw new NotImplementedException($"Request [{JsonSerializer.Serialize(request)}]");
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            CacheManager.ClearRegion(user.CacheRegion);

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok
                }
            };
        }

        #region Privates

        private Task<string[]> AllowedUsers()
        {
            return CacheManager.GetAsync(CacheManagerBase.SystemCacheRegionUrn + ":active_usernames", async () => await DbContext.Users.Where(x => x.IsActive ?? false).Select(x => x.UserName).ToArrayAsync().ConfigureAwait(false), CacheManagerBase.SystemCacheRegionUrn);
        }

        //private async Task<string[]> AllowedUsers()
        //{
        //    return await CacheManager.GetAsync(CacheManagerBase.SystemCacheRegionUrn + ":active_usernames", async () =>
        //    {
        //        return await DbContext.Users.Where(x => x.IsActive ?? false).Select(x => x.UserName).ToArrayAsync().ConfigureAwait(false);
        //    }, CacheManagerBase.SystemCacheRegionUrn).ConfigureAwait(false);
        //}

        private subsonic.MusicFolder CollectionMusicFolder()
        {
            return MusicFolders().First(x => x.id == 1);
        }

        private async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetArtistsAction(subsonic.Request request, Library.Models.Users.User roadieUser)
        {
            var indexes = new List<subsonic.IndexID3>();
            var musicFolder = MusicFolders().Find(x => x.id == (request.MusicFolderId ?? 2));
            var pagedRequest = request.PagedRequest;
            if (musicFolder == CollectionMusicFolder())
            {
                // Indexes for "Collection" Artists alphabetically
            }
            else
            {
                // Indexes for "Music" Artists alphabetically
                pagedRequest.SkipValue = 0;
                pagedRequest.Limit = short.MaxValue;
                pagedRequest.Sort = "Artist.Text";
                var artistList = await ArtistService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
                foreach (var artistGroup in artistList.Rows.GroupBy(x => x.Artist.Text.Substring(0, 1)))
                {
                    indexes.Add(new subsonic.IndexID3
                    {
                        name = artistGroup.Key,
                        artist = SubsonicArtistID3sForArtists(artistGroup)
                    });
                }
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.artists,
                    Item = new subsonic.ArtistsID3
                    {
                        index = indexes.ToArray()
                    }
                }
            };
        }

        private async Task<subsonic.SubsonicOperationResult<subsonic.Response>> GetIndexesAction(
            subsonic.Request request, Library.Models.Users.User roadieUser, long? ifModifiedSince = null)
        {
            var modifiedSinceFilter = ifModifiedSince?.FromUnixTime();
            var musicFolderFilter = !request.MusicFolderId.HasValue
                ? new subsonic.MusicFolder()
                : MusicFolders().Find(x => x.id == request.MusicFolderId.Value);
            var indexes = new List<subsonic.Index>();

            if (musicFolderFilter.id == CollectionMusicFolder().id)
            {
                // Collections for Music Folders by Alpha First
                foreach (var collectionFirstLetter in (from c in DbContext.Collections
                                                       let first = c.Name.Substring(0, 1)
                                                       orderby first
                                                       select first).Distinct().ToArray())
                {
                    indexes.Add(new subsonic.Index
                    {
                        name = collectionFirstLetter,
                        artist = (from c in DbContext.Collections
                                  where c.Name.Substring(0, 1) == collectionFirstLetter
                                  where modifiedSinceFilter == null || c.LastUpdated >= modifiedSinceFilter
                                  orderby c.SortName, c.Name
                                  select new subsonic.Artist
                                  {
                                      id = subsonic.Request.CollectionIdentifier + c.RoadieId,
                                      name = c.Name,
                                      artistImageUrl = ImageHelper.MakeCollectionThumbnailImage(Configuration, HttpContext, c.RoadieId).Url,
                                      averageRating = 0,
                                      userRating = 0
                                  }).ToArray()
                    });
                }
            }
            else
            {
                // Indexes for Artists alphabetically
                var pagedRequest = request.PagedRequest;
                pagedRequest.SkipValue = 0;
                pagedRequest.Limit = short.MaxValue;
                pagedRequest.Sort = "Artist.Text";
                var artistList = await ArtistService.ListAsync(roadieUser, pagedRequest).ConfigureAwait(false);
                foreach (var artistGroup in artistList.Rows.GroupBy(x => x.Artist.Text.Substring(0, 1)))
                {
                    indexes.Add(new subsonic.Index
                    {
                        name = artistGroup.Key,
                        artist = SubsonicArtistsForArtists(artistGroup)
                    });
                }
            }

            return new subsonic.SubsonicOperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.indexes,
                    Item = new subsonic.Indexes
                    {
                        lastModified = DateTime.UtcNow.ToUnixTime(),
                        index = indexes.ToArray()
                    }
                }
            };
        }

        private List<subsonic.MusicFolder> MusicFolders()
        {
            return new List<subsonic.MusicFolder>
            {
                new subsonic.MusicFolder {id = 1, name = "Collections"},
                new subsonic.MusicFolder {id = 2, name = "Music"}
            };
        }

        private new async Task<subsonic.SubsonicOperationResult<bool>> SetArtistRating(Guid artistId, Library.Identity.User user, short rating)
        {
            var r = await base.SetArtistRating(artistId, user, rating).ConfigureAwait(false);
            if (r.IsNotFoundResult)
            {
                return new subsonic.SubsonicOperationResult<bool>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Artist Id [{artistId}]");
            }
            return new subsonic.SubsonicOperationResult<bool>
            {
                IsSuccess = r.IsSuccess,
                Data = r.IsSuccess
            };
        }

        private new async Task<subsonic.SubsonicOperationResult<bool>> SetReleaseRating(Guid releaseId,
            Library.Identity.User user, short rating)
        {
            var r = await base.SetReleaseRating(releaseId, user, rating).ConfigureAwait(false);
            if (r.IsNotFoundResult)
            {
                return new subsonic.SubsonicOperationResult<bool>(subsonic.ErrorCodes.TheRequestedDataWasNotFound,
                   $"Invalid Release Id [{releaseId}]");
            }

            return new subsonic.SubsonicOperationResult<bool>
            {
                IsSuccess = r.IsSuccess,
                Data = r.IsSuccess
            };
        }

        private new async Task<subsonic.SubsonicOperationResult<bool>> SetTrackRating(Guid trackId,
            Library.Identity.User user, short rating)
        {
            var r = await base.SetTrackRating(trackId, user, rating).ConfigureAwait(false);
            if (r.IsNotFoundResult)
            {
                return new subsonic.SubsonicOperationResult<bool>(subsonic.ErrorCodes.TheRequestedDataWasNotFound,
                   $"Invalid Track Id [{trackId}]");
            }

            return new subsonic.SubsonicOperationResult<bool>
            {
                IsSuccess = r.IsSuccess,
                Data = r.IsSuccess
            };
        }

        private subsonic.AlbumID3 SubsonicAlbumID3ForRelease(ReleaseList r)
        {
            return new subsonic.AlbumID3
            {
                id = $"{subsonic.Request.ReleaseIdIdentifier}{r.Id}",
                artistId = r.Artist.Value,
                name = r.Release.Text,
                songCount = r.TrackCount ?? 0,
                duration = r.Duration.ToSecondsFromMilliseconds(),
                artist = r.Artist.Text,
                coverArt = $"{subsonic.Request.ReleaseIdIdentifier}{r.Id}",
                created = r.CreatedDate.Value,
                genre = r.Genre.Text,
                playCount = r.TrackPlayedCount ?? 0,
                playCountSpecified = true,
                starred = r.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = r.UserRating?.IsFavorite ?? false,
                year = SafeParser.ToNumber<int>(r.ReleaseYear),
                yearSpecified = true
            };
        }

        private subsonic.AlbumID3[] SubsonicAlbumID3ForReleases(IEnumerable<ReleaseList> r)
        {
            if (r?.Any() != true)
            {
                return new subsonic.AlbumID3[0];
            }

            return r.Select(x => SubsonicAlbumID3ForRelease(x)).ToArray();
        }

        private subsonic.Artist SubsonicArtistForArtist(ArtistList artist)
        {
            return new subsonic.Artist
            {
                id = $"{subsonic.Request.ArtistIdIdentifier}{artist.Artist.Value}",
                name = artist.Artist.Text,
                artistImageUrl = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, artist.Id).Url,
                averageRating = artist.Rating ?? 0,
                averageRatingSpecified = true,
                starred = artist.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = artist.UserRating?.IsFavorite ?? false,
                userRating = artist.UserRating != null ? artist.UserRating.Rating ?? 0 : 0,
                userRatingSpecified = artist.UserRating?.Rating != null
            };
        }

        private subsonic.ArtistID3 SubsonicArtistID3ForArtist(ArtistList artist)
        {
            var artistImageUrl = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, artist.Id).Url;
            return new subsonic.ArtistID3
            {
                id = $"{subsonic.Request.ArtistIdIdentifier}{artist.Artist.Value}",
                name = artist.Artist.Text,
                albumCount = artist.ReleaseCount ?? 0,
                coverArt = artistImageUrl,
                artistImageUrl = artistImageUrl,
                starred = artist.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = artist.UserRating?.IsFavorite ?? false
            };
        }

        private subsonic.ArtistID3[] SubsonicArtistID3sForArtists(IEnumerable<ArtistList> artists)
        {
            if (artists?.Any() != true)
            {
                return new subsonic.ArtistID3[0];
            }

            return artists.Select(x => SubsonicArtistID3ForArtist(x)).ToArray();
        }

        private subsonic.ArtistInfo2 SubsonicArtistInfo2InfoForArtist(data.Artist artist)
        {
            return new subsonic.ArtistInfo2
            {
                biography = artist.BioContext,
                largeImageUrl = ImageHelper.MakeImage(Configuration, HttpContext, artist.RoadieId, nameof(artist), Configuration.LargeImageSize).Url,
                mediumImageUrl = ImageHelper.MakeImage(Configuration, HttpContext, artist.RoadieId, nameof(artist), Configuration.MediumImageSize).Url,
                musicBrainzId = artist.MusicBrainzId,
                similarArtist = new subsonic.ArtistID3[0],
                smallImageUrl = ImageHelper.MakeImage(Configuration, HttpContext, artist.RoadieId, nameof(artist), Configuration.SmallImageSize).Url
            };
        }

        private subsonic.ArtistInfo SubsonicArtistInfoForArtist(data.Artist artist)
        {
            return new subsonic.ArtistInfo
            {
                biography = artist.BioContext,
                largeImageUrl = ImageHelper.MakeImage(Configuration, HttpContext, artist.RoadieId, nameof(artist), Configuration.LargeImageSize).Url,
                mediumImageUrl = ImageHelper.MakeImage(Configuration, HttpContext, artist.RoadieId, nameof(artist), Configuration.MediumImageSize).Url,
                musicBrainzId = artist.MusicBrainzId,
                similarArtist = new subsonic.Artist[0],
                smallImageUrl = ImageHelper.MakeImage(Configuration, HttpContext, artist.RoadieId, nameof(artist), Configuration.SmallImageSize).Url
            };
        }

        private subsonic.Artist[] SubsonicArtistsForArtists(IEnumerable<ArtistList> artists)
        {
            if (artists?.Any() != true)
            {
                return new subsonic.Artist[0];
            }

            return artists.Select(x => SubsonicArtistForArtist(x)).ToArray();
        }

        private subsonic.ArtistWithAlbumsID3 SubsonicArtistWithAlbumsID3ForArtist(ArtistList artist, subsonic.AlbumID3[] releases)
        {
            var artistImageUrl = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, artist.Id).Url;
            return new subsonic.ArtistWithAlbumsID3
            {
                id = $"{subsonic.Request.ArtistIdIdentifier}{artist.Artist.Value}",
                album = releases,
                albumCount = releases.Length,
                artistImageUrl = artistImageUrl,
                coverArt = artistImageUrl,
                name = artist.Artist.Text,
                starred = artist.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = artist.UserRating?.IsFavorite ?? false
            };
        }

        private subsonic.Bookmark SubsonicBookmarkForBookmark(BookmarkList b, subsonic.Child entry)
        {
            return new subsonic.Bookmark
            {
                changed = b.LastUpdated ?? b.CreatedDate.Value,
                comment = b.Comment,
                created = b.CreatedDate.Value,
                position = b.Position ?? 0,
                username = b.User.Text,
                entry = entry
            };
        }

        private subsonic.Bookmark[] SubsonicBookmarksForBookmarks(IEnumerable<BookmarkList> bb,
            IEnumerable<TrackList> childTracks)
        {
            if (bb?.Any() != true)
            {
                return new subsonic.Bookmark[0];
            }

            var result = new List<subsonic.Bookmark>();
            foreach (var bookmark in bb)
            {
                subsonic.Child child = null;
                switch (bookmark.Type.Value)
                {
                    case BookmarkType.Track:
                        child = SubsonicChildForTrack(childTracks.FirstOrDefault(x => x.Id == SafeParser.ToGuid(bookmark.Bookmark.Value)));
                        break;

                    default:
                        throw new NotImplementedException("Wrong Bookmark type to convert to Subsonic media Bookmark");
                }

                result.Add(SubsonicBookmarkForBookmark(bookmark, child));
            }

            return result.ToArray();
        }

        private subsonic.Child SubsonicChildForRelease(ReleaseList r, string parent, string path)
        {
            return new subsonic.Child
            {
                id = $"{subsonic.Request.ReleaseIdIdentifier}{r.Id}",
                album = r.Release.Text,
                albumId = $"{subsonic.Request.ReleaseIdIdentifier}{r.Id}",
                artist = r.Artist.Text,
                averageRating = r.Rating ?? 0,
                averageRatingSpecified = true,
                coverArt = $"{subsonic.Request.ReleaseIdIdentifier}{r.Id}",
                created = r.CreatedDate.Value,
                createdSpecified = true,
                genre = r.Genre.Text,
                isDir = true,
                parent = parent ?? subsonic.Request.ArtistIdIdentifier + r.Artist.Value,
                path = path,
                playCount = r.TrackPlayedCount ?? 0,
                playCountSpecified = true,
                starred = r.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = r.UserRating?.IsFavorite ?? false,
                title = r.Release.Text,
                userRating = r.UserRating != null ? r.UserRating.Rating ?? 0 : 0,
                userRatingSpecified = r.UserRating?.Rating != null,
                year = SafeParser.ToNumber<int>(r.ReleaseYear),
                yearSpecified = true
            };
        }

        private subsonic.Child SubsonicChildForTrack(TrackList t)
        {
            var userRating = t.UserRating?.Rating ?? 0;
            if (userRating > 0)
            {
                // This is done as many subsonic apps think rating "1" is don't play song, versus a minimum indication of like as intended for Roadie.
                // To disable this set the configuration SubsonicRatingBoost to 0
                userRating += Configuration.SubsonicRatingBoost ?? 1;
                userRating = userRating > 5 ? (short)5 : userRating;
            }
            return new subsonic.Child
            {
                id = $"{subsonic.Request.TrackIdIdentifier}{t.Id}",
                album = t.Release.Release.Text,
                albumId = $"{subsonic.Request.ReleaseIdIdentifier}{t.Release.Release.Value}",
                artist = t.Artist.Artist.Text,
                artistId = $"{subsonic.Request.ArtistIdIdentifier}{t.Artist.Artist.Value}",
                averageRating = t.Rating ?? 0,
                averageRatingSpecified = true,
                bitRate = 320,
                bitRateSpecified = true,
                contentType = MimeTypeHelper.Mp3MimeType,
                coverArt = $"{subsonic.Request.TrackIdIdentifier}{t.Id}",
                created = t.CreatedDate.Value,
                createdSpecified = true,
                discNumber = t.MediaNumber ?? 0,
                discNumberSpecified = true,
                duration = t.Duration.ToSecondsFromMilliseconds(),
                durationSpecified = true,
                isDir = false,
                parent = $"{subsonic.Request.ReleaseIdIdentifier}{t.Release.Release.Value}",
                path = $"{t.Artist.Artist.Text}/{t.Release.Release.Text}/{t.TrackNumber} - {t.Title}.mp3",
                playCountSpecified = true,
                size = t.FileSize ?? 0,
                sizeSpecified = true,
                starred = t.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = t.UserRating?.IsFavorite ?? false,
                suffix = "mp3",
                title = t.Title,
                track = t.TrackNumber ?? 0,
                trackSpecified = t.TrackNumber.HasValue,
                type = subsonic.MediaType.music,
                typeSpecified = true,
                userRating = userRating,
                userRatingSpecified = t.UserRating != null,
                year = t.Year ?? 0,
                yearSpecified = t.Year.HasValue,
                transcodedContentType = MimeTypeHelper.Mp3MimeType,
                transcodedSuffix = "mp3",
                isVideo = false,
                isVideoSpecified = true,
                playCount = t.PlayedCount ?? 0
            };
        }

        private subsonic.Child[] SubsonicChildrenForReleases(IEnumerable<ReleaseList> r, string parent)
        {
            if (r?.Any() != true)
            {
                return new subsonic.Child[0];
            }
            return r.Select(x => SubsonicChildForRelease(x, parent, $"{x.Artist.Text}/{x.Release.Text}/")).ToArray();
        }

        private subsonic.Child[] SubsonicChildrenForTracks(IEnumerable<TrackList> tracks)
        {
            if (tracks?.Any() != true)
            {
                return new subsonic.Child[0];
            }
            return tracks.Select(x => SubsonicChildForTrack(x)).ToArray();
        }

        private async Task<subsonic.Playlist> SubsonicPlaylistForPlaylist(PlaylistList playlist, IEnumerable<TrackList> playlistTracks = null)
        {
            return new subsonic.PlaylistWithSongs
            {
                coverArt = ImageHelper.MakePlaylistThumbnailImage(Configuration, HttpContext, playlist.Id).Url,
                allowedUser = playlist.IsPublic ? await AllowedUsers().ConfigureAwait(false) : null,
                changed = playlist.LastUpdated ?? playlist.CreatedDate ?? DateTime.UtcNow,
                created = playlist.CreatedDate ?? DateTime.UtcNow,
                duration = playlist.Duration.ToSecondsFromMilliseconds(),
                id = $"{subsonic.Request.PlaylistdIdentifier}{playlist.Id}",
                name = playlist.Playlist.Text,
                owner = playlist.User.Text,
                @public = playlist.IsPublic,
                publicSpecified = true,
                songCount = playlist.PlaylistCount ?? 0,
                entry = SubsonicChildrenForTracks(playlistTracks)
            };
        }

        private async Task<subsonic.Playlist[]> SubsonicPlaylistsForPlaylists(IEnumerable<PlaylistList> playlists)
        {
            if (playlists?.Any() != true)
            {
                return new subsonic.Playlist[0];
            }
            var result = new List<subsonic.Playlist>();
            foreach (var playlist in playlists)
            {
                result.Add(await SubsonicPlaylistForPlaylist(playlist).ConfigureAwait(false));
            }
            return result.ToArray();
        }

        private async Task<subsonic.User> SubsonicUserForUser(Library.Identity.User user)
        {
            var isAdmin = await UserManger.IsInRoleAsync(user, "Admin").ConfigureAwait(false);
            var isEditor = await UserManger.IsInRoleAsync(user, "Editor").ConfigureAwait(false);
            return new subsonic.User
            {
                adminRole = false, // disabling this as we dont want Roadie user management done via Subsonic API
                avatarLastChanged = user.LastUpdated ?? user.CreatedDate ?? DateTime.UtcNow,
                avatarLastChangedSpecified = user.LastUpdated.HasValue,
                commentRole = true,
                coverArtRole = isEditor || isAdmin,
                downloadRole = isEditor || isAdmin,
                email = user.Email,
                jukeboxRole = false, // Jukebox disabled (what is jukebox?)
                maxBitRate = 320,
                maxBitRateSpecified = true,
                playlistRole = isEditor || isAdmin,
                podcastRole = false, // Disable podcast nonsense
                scrobblingEnabled = false, // Disable scrobbling
                settingsRole = isAdmin,
                shareRole = false,
                streamRole = true,
                uploadRole = true,
                username = user.UserName,
                videoConversionRole = false, // Disable video nonsense
                folder = MusicFolders().Select(x => x.id).ToArray()
            };
        }

        private async Task<subsonic.SubsonicOperationResult<bool>> ToggleArtistStar(Guid artistId, Library.Identity.User user, bool starred)
        {
            var r = await ToggleArtistFavorite(artistId, user, starred).ConfigureAwait(false);
            if (r.IsNotFoundResult)
            {
                return new subsonic.SubsonicOperationResult<bool>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Artist Id [{artistId}]");
            }
            return new subsonic.SubsonicOperationResult<bool>
            {
                IsSuccess = r.IsSuccess,
                Data = r.IsSuccess
            };
        }

        private async Task<subsonic.SubsonicOperationResult<bool>> ToggleReleaseStar(Guid releaseId, Library.Identity.User user, bool starred)
        {
            var r = await ToggleReleaseFavorite(releaseId, user, starred).ConfigureAwait(false);
            if (r.IsNotFoundResult)
            {
                return new subsonic.SubsonicOperationResult<bool>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Release Id [{releaseId}]");
            }
            return new subsonic.SubsonicOperationResult<bool>
            {
                IsSuccess = r.IsSuccess,
                Data = r.IsSuccess
            };
        }

        private async Task<subsonic.SubsonicOperationResult<bool>> ToggleTrackStar(Guid trackId, Library.Identity.User user, bool starred)
        {
            var r = await ToggleTrackFavorite(trackId, user, starred).ConfigureAwait(false);
            if (r.IsNotFoundResult)
            {
                return new subsonic.SubsonicOperationResult<bool>(subsonic.ErrorCodes.TheRequestedDataWasNotFound, $"Invalid Track Id [{trackId}]");
            }
            return new subsonic.SubsonicOperationResult<bool>
            {
                IsSuccess = r.IsSuccess,
                Data = r.IsSuccess
            };
        }

        #endregion Privates
    }
}