using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;
using subsonic = Roadie.Library.Models.ThirdPartyApi.Subsonic;

namespace Roadie.Api.Services
{
    /// <summary>
    /// Subsonic API emulator for Roadie. Enables Subsonic clients to work with Roadie.
    /// <seealso cref="http://www.subsonic.org/pages/inc/api/schema/subsonic-rest-api-1.16.1.xsd">
    /// <seealso cref="http://www.subsonic.org/pages/api.jsp#getIndexes"/>
    /// <!-- Generated the classes from the schema above using 'xsd subsonic-rest-api-1.16.1.xsd /c /f /n:Roadie.Library.Models.Subsonic' from Visual Studio Command Prompt -->
    /// </summary>
    public class SubsonicService : ServiceBase, ISubsonicService
    {
        public const string SubsonicVersion = "1.16.1";

        private IArtistService ArtistService { get; }
        private IImageService ImageService { get; }
        private IReleaseService ReleaseService { get; }
        private ITrackService TrackService { get; }

        public SubsonicService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<SubsonicService> logger,
                             IArtistService artistService,
                             ITrackService trackService,
                             ICollectionService collectionService,
                             IPlaylistService playlistService,
                             IReleaseService releaseService,
                             IImageService imageService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            this.ArtistService = artistService;
            this.ReleaseService = releaseService;
            this.TrackService = trackService;
            this.ImageService = imageService;
        }

        /// <summary>
        /// Returns a list of random, newest, highest rated etc. albums. Similar to the album lists on the home page of the Subsonic web interface.
        /// </summary>
        public async Task<OperationResult<subsonic.Response>> GetAlbumList(subsonic.Request request, User roadieUser, string version)
        {
            var releaseResult = new Library.Models.Pagination.PagedResult<ReleaseList>();

            switch (request.Type)
            {
                case subsonic.ListType.Random:
                    releaseResult = await this.ReleaseService.List(roadieUser, request.PagedRequest, true);
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
                    releaseResult = await this.ReleaseService.List(roadieUser, request.PagedRequest);
                    break;

                default:
                    return new OperationResult<subsonic.Response>($"Unknown Album List Type [{ request.Type}]");
            }

            if (!releaseResult.IsSuccess)
            {
                return new OperationResult<subsonic.Response>(releaseResult.Message);
            }

            var albums = this.SubsonicChildrenForReleases(releaseResult.Rows, null);

            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.albumList,
                    Item = new subsonic.AlbumList
                    {
                        album = albums
                    }
                }
            };
        }


        /// <summary>
        /// Returns a cover art image.
        /// </summary>
        public async Task<FileOperationResult<Roadie.Library.Models.Image>> GetCoverArt(subsonic.Request request, int? size)
        {
            var sw = Stopwatch.StartNew();
            var result = new FileOperationResult<Roadie.Library.Models.Image>
            {
                Data = new Roadie.Library.Models.Image()
            };

            if (request.ArtistId != null)
            {
                var artistImage = await this.ImageService.ArtistThumbnail(request.ArtistId.Value, size, size);
                if (!artistImage.IsSuccess)
                {
                    return artistImage;
                }
                result.Data.Bytes = artistImage.Data.Bytes;
            }
            else if (request.TrackId != null)
            {
                var trackimage = await this.ImageService.TrackThumbnail(request.TrackId.Value, size, size);
                if (!trackimage.IsSuccess)
                {
                    return trackimage;
                }
                result.Data.Bytes = trackimage.Data.Bytes;
            }
            else if (request.CollectionId != null)
            {
                var collection = this.GetCollection(request.CollectionId.Value);
                if (collection == null)
                {
                    return new FileOperationResult<Roadie.Library.Models.Image>(true, $"Invalid CollectionId [{ request.CollectionId}]");
                }
                result.Data.Bytes = collection.Thumbnail;
            }
            else if (request.ReleaseId != null)
            {
                var release = this.GetRelease(request.ReleaseId.Value);
                if (release == null)
                {
                    return new FileOperationResult<Roadie.Library.Models.Image>(true, $"Invalid ReleaseId [{ request.ReleaseId}]");
                }
                result.Data.Bytes = release.Thumbnail;
            }
            else if (request.PlaylistId != null)
            {
                var playlist = this.GetPlaylist(request.PlaylistId.Value);
                if (playlist == null)
                {
                    return new FileOperationResult<Roadie.Library.Models.Image>(true, $"Invalid PlaylistId [{ request.PlaylistId}]");
                }
                result.Data.Bytes = playlist.Thumbnail;
            }
            else if (!string.IsNullOrEmpty(request.u))
            {
                var user = this.GetUser(request.u);
                if (user == null)
                {
                    return new FileOperationResult<Roadie.Library.Models.Image>(true, $"Invalid Username [{ request.u}]");
                }
                result.Data.Bytes = user.Avatar;
            }

            if (size.HasValue && result.Data.Bytes != null)
            {
                result.Data.Bytes = ImageHelper.ResizeImage(result.Data.Bytes, size.Value, size.Value);
                result.ETag = EtagHelper.GenerateETag(this.HttpEncoder, result.Data.Bytes);
                result.LastModified = DateTime.UtcNow;
            }
            result.IsSuccess = result.Data.Bytes != null;
            sw.Stop();
            return new FileOperationResult<Roadie.Library.Models.Image>(result.Messages)
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
        /// Returns all genres
        /// </summary>
        public async Task<OperationResult<subsonic.Response>> GetGenres(subsonic.Request request)
        {
            var genres = (from g in this.DbContext.Genres
                          let albumCount = (from rg in this.DbContext.ReleaseGenres
                                            where rg.GenreId == g.Id
                                            select rg.Id).Count()
                          let songCount = (from rg in this.DbContext.ReleaseGenres
                                           join rm in this.DbContext.ReleaseMedias on rg.ReleaseId equals rm.ReleaseId
                                           join t in this.DbContext.Tracks on rm.ReleaseId equals t.ReleaseMediaId
                                           where rg.GenreId == g.Id
                                           select t.Id).Count()
                          select new subsonic.Genre
                          {
                              songCount = songCount,
                              albumCount = albumCount,
                              value = g.Name
                          }).OrderBy(x => x.value).ToArray();

            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.genres,
                    Item = new subsonic.Genres
                    {
                        genre = genres.ToArray()
                    }
                }
            };
        }

        /// <summary>
        /// Returns an indexed structure of all artists.
        /// </summary>
        /// <param name="request">Query from application.</param>
        /// <param name="musicFolderId">If specified, only return artists in the music folder with the given ID.</param>
        /// <param name="ifModifiedSince">If specified, only return a result if the artist collection has changed since the given time (in milliseconds since 1 Jan 1970).</param>
        public async Task<OperationResult<subsonic.Response>> GetIndexes(subsonic.Request request, User roadieUser, string musicFolderId = null, long? ifModifiedSince = null)
        {
            var modifiedSinceFilter = ifModifiedSince.HasValue ? (DateTime?)ifModifiedSince.Value.FromUnixTime() : null;
            subsonic.MusicFolder musicFolderFilter = string.IsNullOrEmpty(musicFolderId) ? new subsonic.MusicFolder() : this.MusicFolders().FirstOrDefault(x => x.id == SafeParser.ToNumber<int>(musicFolderId));
            var indexes = new List<subsonic.Index>();

            if (musicFolderFilter.id == this.CollectionMusicFolder().id)
            {
                // Collections for Music Folders by Alpha First
                foreach (var collectionFirstLetter in (from c in this.DbContext.Collections
                                                       let first = c.Name.Substring(0, 1)
                                                       orderby first
                                                       select first).Distinct().ToArray())
                {
                    indexes.Add(new subsonic.Index
                    {
                        name = collectionFirstLetter,
                        artist = (from c in this.DbContext.Collections
                                  where c.Name.Substring(0, 1) == collectionFirstLetter
                                  where modifiedSinceFilter == null || c.LastUpdated >= modifiedSinceFilter
                                  orderby c.SortName, c.Name
                                  select new subsonic.Artist
                                  {
                                      id = subsonic.Request.CollectionIdentifier + c.RoadieId.ToString(),
                                      name = c.Name,
                                      artistImageUrl = this.MakeCollectionThumbnailImage(c.RoadieId).Url,
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
                pagedRequest.Limit = int.MaxValue;
                pagedRequest.Sort = "Artist.Text";
                var artistList = await this.ArtistService.List(roadieUser, pagedRequest);
                foreach (var artistGroup in artistList.Rows.GroupBy(x => x.Artist.Text.Substring(0, 1)))
                {
                    indexes.Add(new subsonic.Index
                    {
                        name = artistGroup.Key,
                        artist = this.SubsonicArtistsForArtists(artistGroup)
                    });
                };
            }
            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.indexes,
                    Item = new subsonic.Indexes
                    {
                        index = indexes.ToArray()
                    }
                }
            };
        }

        /// <summary>
        /// Returns a listing of all files in a music directory. Typically used to get list of albums for an artist, or list of songs for an album.
        /// </summary>
        /// <param name="request">Query from application.</param>
        /// <param name="id">A string which uniquely identifies the music folder. Obtained by calls to getIndexes or getMusicDirectory.</param>
        /// <returns></returns>
        public async Task<OperationResult<subsonic.Response>> GetMusicDirectory(subsonic.Request request, User roadieUser, string id)
        {
            var directory = new subsonic.Directory();
            var user = this.GetUser(roadieUser?.UserId);

            // Request to get albums for an Artist
            if (request.ArtistId != null)
            {
                var artist = this.GetArtist(request.ArtistId.Value);
                if (artist == null)
                {
                    return new OperationResult<subsonic.Response>(true, $"Invalid ArtistId [{ request.ArtistId}]");
                }
                directory.id = subsonic.Request.ArtistIdIdentifier + artist.RoadieId.ToString();
                directory.name = artist.Name;
                var artistRating = user?.ArtistRatings?.FirstOrDefault(x => x.ArtistId == artist.Id);
                if (artistRating?.IsFavorite ?? false)
                {
                    directory.starred = (artistRating.LastUpdated ?? artistRating.CreatedDate);
                    directory.starredSpecified = true;
                }
                var pagedRequest = request.PagedRequest;
                pagedRequest.FilterToArtistId = artist.RoadieId;
                var artistReleases = await this.ReleaseService.List(roadieUser, pagedRequest);
                directory.child = this.SubsonicChildrenForReleases(artistReleases.Rows, subsonic.Request.ArtistIdIdentifier + artist.RoadieId.ToString());
            }
            // Request to get albums for in a Collection
            else if (request.CollectionId != null)
            {
                var collection = this.GetCollection(request.CollectionId.Value);
                if (collection == null)
                {
                    return new OperationResult<subsonic.Response>(true, $"Invalid CollectionId [{ request.CollectionId}]");
                }
                directory.id = subsonic.Request.CollectionIdentifier + collection.RoadieId.ToString();
                directory.name = collection.Name;
                var pagedRequest = request.PagedRequest;
                pagedRequest.FilterToCollectionId = collection.RoadieId;
                var collectionReleases = await this.ReleaseService.List(roadieUser, pagedRequest);
                directory.child = this.SubsonicChildrenForReleases(collectionReleases.Rows, subsonic.Request.CollectionIdentifier + collection.RoadieId.ToString());
            }
            // Request to get Tracks for an Album
            else if(request.ReleaseId.HasValue)
            {
                var release = this.GetRelease(request.ReleaseId.Value);
                if (release == null)
                {
                    return new OperationResult<subsonic.Response>(true, $"Invalid ReleaseId [{ request.ReleaseId}]");
                }
                directory.id = subsonic.Request.ReleaseIdIdentifier + release.RoadieId.ToString();
                directory.name = release.Title;
                var releaseRating = user?.ReleaseRatings?.FirstOrDefault(x => x.ReleaseId == release.Id);
                directory.averageRating = release.Rating ?? 0;
                directory.parent = subsonic.Request.ArtistIdIdentifier + release.Artist.RoadieId.ToString();
                if (releaseRating?.IsFavorite ?? false)
                {
                    directory.starred = releaseRating.LastUpdated ?? releaseRating.CreatedDate;
                    directory.starredSpecified = true;
                }
                var pagedRequest = request.PagedRequest;
                var songTracks = await this.TrackService.List(roadieUser, pagedRequest, false, release.RoadieId);
                directory.child = this.SubsonicChildrenForTracks(songTracks.Rows);
                directory.playCount = directory.child.Select(x => x.playCount).Sum();
            }
            else
            {
                return new OperationResult<subsonic.Response>($"Unknown GetMusicDirectory Type [{ JsonConvert.SerializeObject(request) }], id [{ id }]");
            }
            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.directory,
                    Item = directory
                }
            };
        }

        /// <summary>
        /// Returns all configured top-level music folders. Takes no extra parameters.
        /// </summary>
        public async Task<OperationResult<subsonic.Response>> GetMusicFolders(subsonic.Request request)
        {
            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.musicFolders,
                    Item = new subsonic.MusicFolders
                    {
                        musicFolder = this.MusicFolders().ToArray()
                    }
                }
            };
        }

        /// <summary>
        /// Returns all playlists a user is allowed to play.
        /// </summary>
        public async Task<OperationResult<subsonic.Response>> GetPlaylists(subsonic.Request request, User roadieUser, string filterToUserName)
        {
            var playlists = (from playlist in this.DbContext.Playlists
                             join u in this.DbContext.Users on playlist.UserId equals u.Id
                             let trackCount = (from pl in this.DbContext.PlaylistTracks
                                               where pl.PlayListId == playlist.Id
                                               select pl.Id).Count()
                             let playListDuration = (from pl in this.DbContext.PlaylistTracks
                                                     join t in this.DbContext.Tracks on pl.TrackId equals t.Id
                                                     where pl.PlayListId == playlist.Id
                                                     select t.Duration).Sum()
                             where (playlist.IsPublic) || (roadieUser != null && playlist.UserId == roadieUser.Id)
                             select new subsonic.Playlist
                             {
                                 id = playlist.RoadieId.ToString(),
                                 name = playlist.Name,
                                 comment = playlist.Description,
                                 owner = u.UserName,
                                 songCount = trackCount,
                                 duration = playListDuration ?? 0,
                                 created = playlist.CreatedDate,
                                 changed = playlist.LastUpdated ?? playlist.CreatedDate,
                                 coverArt = this.MakePlaylistThumbnailImage(playlist.RoadieId).Url,
                                 @public = playlist.IsPublic,
                                 publicSpecified = playlist.IsPublic
                             }
                     );

            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.playlists,
                    Item = new subsonic.Playlists
                    {
                        playlist = playlists.ToArray()
                    }
                }
            };
        }

        /// <summary>
        /// Returns all Podcast channels the server subscribes to, and (optionally) their episodes. This method can also be used to return details for only one channel - refer to the id parameter. A typical use case for this method would be to first retrieve all channels without episodes, and then retrieve all episodes for the single channel the user selects.
        /// </summary>
        public async Task<OperationResult<subsonic.Response>> GetPodcasts(subsonic.Request request)
        {
            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.podcasts,
                    Item = new object[0]
                }
            };
        }

        /// <summary>
        /// Used to test connectivity with the server. Takes no extra parameters.
        /// </summary>
        public OperationResult<subsonic.Response> Ping(subsonic.Request request)
        {
            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok                    
                }
            };
        }

        /// <summary>
        /// Returns albums, artists and songs matching the given search criteria. Supports paging through the result.
        /// </summary>
        public async Task<OperationResult<subsonic.Response>> Search(subsonic.Request request, User roadieUser)
        {
            var query = this.HttpEncoder.UrlDecode(request.Query);

            // Search artists with query returning ArtistCount skipping ArtistOffset
            var artistPagedRequest = request.PagedRequest;
            artistPagedRequest.Sort = "Artist.Text";
            artistPagedRequest.Limit = request.ArtistCount ?? artistPagedRequest.Limit;
            artistPagedRequest.SkipValue = request.ArtistOffset ?? artistPagedRequest.SkipValue;
            artistPagedRequest.Filter = query;
            var artistResult = await this.ArtistService.List(roadieUser, artistPagedRequest);
            var artists = this.SubsonicArtistsForArtists(artistResult.Rows);

            // Search release with query returning RelaseCount skipping ReleaseOffset
            var releasePagedRequest = request.PagedRequest;
            releasePagedRequest.Sort = "Release.Text";
            releasePagedRequest.Limit = request.AlbumCount ?? releasePagedRequest.Limit;
            releasePagedRequest.SkipValue = request.AlbumOffset ?? releasePagedRequest.SkipValue;
            releasePagedRequest.Filter = query;
            var releaseResult = await this.ReleaseService.List(roadieUser, releasePagedRequest);
            var albums = this.SubsonicChildrenForReleases(releaseResult.Rows, null);

            // Search tracks with query returning SongCount skipping SongOffset
            var trackPagedRequest = request.PagedRequest;
            trackPagedRequest.Sort = "Track.Text";
            trackPagedRequest.Limit = request.SongCount ?? trackPagedRequest.Limit;
            trackPagedRequest.SkipValue = request.SongOffset ?? trackPagedRequest.SkipValue;
            trackPagedRequest.Filter = query;
            var songResult = await this.TrackService.List(roadieUser, trackPagedRequest);
            var songs = this.SubsonicChildrenForTracks(songResult.Rows);

            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.searchResult2,
                    Item = new subsonic.SearchResult2
                    {
                        artist = artists.ToArray(),
                        album = albums.ToArray(),
                        song = songs.ToArray()
                    }
                }
            };
        }

        public async Task<OperationResult<subsonic.Response>> GetAlbum(subsonic.Request request, User roadieUser)
        {
            if(!request.ReleaseId.HasValue)
            {
                return new OperationResult<subsonic.Response>(true, $"Invalid Release [{ request.ReleaseId}]");
            }
            var release = this.GetRelease(request.ReleaseId.Value);
            if(release == null)
            {
                return new OperationResult<subsonic.Response>(true, $"Invalid Release [{ request.ReleaseId}]");
            }
            var pagedRequest = request.PagedRequest;
            var releaseTracks = await this.TrackService.List(roadieUser, pagedRequest, false, request.ReleaseId);
            var userRelease = roadieUser == null ? null : this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == roadieUser.Id);
            var genre = release.Genres.FirstOrDefault();
            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.album,
                    Item = new subsonic.AlbumWithSongsID3
                    {
                        artist = release.Artist.Name,
                        artistId = subsonic.Request.ArtistIdIdentifier + release.Artist.RoadieId.ToString(),
                        coverArt = subsonic.Request.ReleaseIdIdentifier + release.RoadieId.ToString(),
                        created = release.CreatedDate,
                        duration = releaseTracks.Rows.Sum(x => x.Duration) ?? 0,
                        genre = genre == null ? null : genre.Genre.Name,
                        id = subsonic.Request.ReleaseIdIdentifier + release.RoadieId.ToString(),
                        name = release.Title,
                        playCount = releaseTracks.Rows.Sum(x => x.PlayedCount) ?? 0,
                        playCountSpecified = releaseTracks.Rows.Any(),
                        songCount = releaseTracks.Rows.Count(),
                        starred = userRelease?.LastUpdated ?? userRelease?.CreatedDate ?? DateTime.UtcNow,
                        starredSpecified = userRelease?.IsFavorite ?? false,
                        year = release.ReleaseDate != null ? release.ReleaseDate.Value.Year : 0,
                        yearSpecified = release.ReleaseDate != null,
                        song = this.SubsonicChildrenForTracks(releaseTracks.Rows)
                    }
                }
            };
        }

        public OperationResult<subsonic.Response> GetLicense(subsonic.Request request)
        {
            return new OperationResult<subsonic.Response>
            {
                IsSuccess = true,
                Data = new subsonic.Response
                {
                    version = SubsonicService.SubsonicVersion,
                    status = subsonic.ResponseStatus.ok,
                    ItemElementName = subsonic.ItemChoiceType.license,
                    Item = new subsonic.License
                    {
                        email = this.Configuration.SmtpFromAddress,
                        valid = true,
                        licenseExpires = DateTime.UtcNow.AddYears(1),
                        licenseExpiresSpecified = true                         
                    }
                }
            };

        }

        /// <summary>
        /// Returns artist info with biography, image URLs and similar artists, using data from last.fm.
        /// </summary>
        public OperationResult<subsonic.Response> GetArtistInfo(subsonic.Request request, string id, int? count, bool includeNotPresent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the avatar (personal image) for a user.
        /// </summary>
        public OperationResult<subsonic.Response> GetAvatar(subsonic.Request request, string username)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns random songs matching the given criteria.
        /// </summary>
        public OperationResult<subsonic.Response> GetRandomSongs(subsonic.Request request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get details about a given user, including which authorization roles and folder access it has. Can be used to enable/disable certain features in the client, such as jukebox control.
        /// </summary>
        public OperationResult<subsonic.Response> GetUser(subsonic.Request request, string username)
        {
            throw new NotImplementedException();
        }

        //getAlbumList2
        //getArtists
        //getStarred2
        //search3


        private subsonic.MusicFolder CollectionMusicFolder()
        {
            return this.MusicFolders().First(x => x.id == 1);
        }

        private List<subsonic.MusicFolder> MusicFolders()
        {
            return new List<subsonic.MusicFolder>
            {
                new subsonic.MusicFolder { id = 1, name = "Collections"},
                new subsonic.MusicFolder { id = 2, name = "Music"}
            };
        }

        private subsonic.Artist SubsonicArtistForArtist(ArtistList artist)
        {
            return new subsonic.Artist
            {
                id = subsonic.Request.ArtistIdIdentifier + artist.Artist.Value.ToString(),
                name = artist.Artist.Text,
                artistImageUrl = this.MakeArtistThumbnailImage(artist.Id).Url,
                averageRating = artist.Rating ?? 0,
                averageRatingSpecified = true,
                starred =  artist.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = artist.UserRating?.IsFavorite ?? false,
                userRating = artist.UserRating != null ? artist.UserRating.Rating ?? 0 : 0,
                userRatingSpecified = artist.UserRating != null && artist.UserRating.Rating != null
            };
        }

        private subsonic.Artist[] SubsonicArtistsForArtists(IEnumerable<ArtistList> artists)
        {
            if (artists == null || !artists.Any())
            {
                return new subsonic.Artist[0];
            }
            return artists.Select(x => this.SubsonicArtistForArtist(x)).ToArray();
        }

        private subsonic.Child SubsonicChildForRelease(ReleaseList r, string parent, string path)
        {
            return new subsonic.Child
            {
                id = subsonic.Request.ReleaseIdIdentifier + r.Id.ToString(),
                album = r.Release.Text,
                albumId = subsonic.Request.ReleaseIdIdentifier + r.Id.ToString(),
                artist = r.Artist.Text,
                averageRating = r.Rating ?? 0,
                averageRatingSpecified = true,
                coverArt = subsonic.Request.ReleaseIdIdentifier + r.Id.ToString(),
                created = r.CreatedDate.Value,
                createdSpecified = true,
                genre = r.Genre.Text,
                isDir = true,
                parent = parent ?? $"{ r.Artist.Text}/{ r.Release.Text}/",
                path = path,
                playCount = r.TrackPlayedCount ?? 0,
                playCountSpecified = true,
                starred = r.UserRating?.RatedDate ?? DateTime.UtcNow,
                starredSpecified = r.UserRating?.IsFavorite ?? false,
                title = r.Release.Text,
                userRating = r.UserRating != null ? r.UserRating.Rating ?? 0 : 0,
                userRatingSpecified = r.UserRating != null && r.UserRating.Rating != null,
                year = SafeParser.ToNumber<int>(r.ReleaseYear),
                yearSpecified = true                
            };
        }

        private subsonic.Child SubsonicChildForTrack(TrackList t)
        {
            return new subsonic.Child
            {
                id = subsonic.Request.TrackIdIdentifier + t.Id.ToString(),
                album = t.Release.Text,
                albumId = subsonic.Request.ReleaseIdIdentifier + t.Release.Value,
                artist = t.Artist.Text,
                artistId = subsonic.Request.ArtistIdIdentifier + t.Artist.Value,
                averageRating = t.Rating ?? 0,
                averageRatingSpecified = true,
                bitRate = 320,
                bitRateSpecified = true,
                contentType = "audio/mpeg",
                coverArt = subsonic.Request.TrackIdIdentifier + t.Id.ToString(),
                created = t.CreatedDate.Value,
                createdSpecified = true,
                discNumber = t.MediaNumber ?? 0,
                discNumberSpecified = true,
                duration = t.Duration.ToSecondsFromMilliseconds(),
                durationSpecified = true,
                parent = subsonic.Request.ReleaseIdIdentifier + t.Release.Value,
                path = $"{ t.Artist.Text }/{ t.Release.Text }/{ t.TrackNumber } - { t.Title }.mp3",
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
                userRating = t.UserRating != null ? t.UserRating.Rating ?? 0 : 0,
                userRatingSpecified = t.UserRating != null,
                year = t.Year ?? 0,
                yearSpecified = t.Year.HasValue,
                playCount = (from utt in this.DbContext.UserTracks
                             join tt in this.DbContext.Tracks on utt.TrackId equals tt.Id
                             join rmm in this.DbContext.ReleaseMedias on tt.ReleaseMediaId equals rmm.Id
                             where tt.Id == t.DatabaseId
                             where utt.PlayedCount != null
                             select utt.PlayedCount ?? 0).Sum(),
            };
        }

        private subsonic.Child[] SubsonicChildrenForReleases(IEnumerable<ReleaseList> r, string parent)
        {
            if (r == null || !r.Any())
            {
                return new subsonic.Child[0];
            }
            return r.Select(x => this.SubsonicChildForRelease(x, parent, $"{ x.Artist.Text}/{ x.Release.Text}/")).ToArray();
        }

        private subsonic.Child[] SubsonicChildrenForTracks(IEnumerable<TrackList> tracks)
        {
            if (tracks == null || !tracks.Any())
            {
                return new subsonic.Child[0];
            }
            return tracks.Select(x => this.SubsonicChildForTrack(x)).ToArray();
        }
    }
}