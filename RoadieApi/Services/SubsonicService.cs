using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
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

        public SubsonicService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<SubsonicService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

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

        public List<subsonic.MusicFolder> MusicFolders()
        {
            return new List<subsonic.MusicFolder>
            {
                new subsonic.MusicFolder { id = 1, name = "Collections"},
                new subsonic.MusicFolder { id = 2, name = "Music"}
            };
        }

        public subsonic.MusicFolder CollectionMusicFolder()
        {
            return this.MusicFolders().First(x => x.id == 1);
        }

        /// <summary>
        /// Returns an indexed structure of all artists.
        /// </summary>
        /// <param name="request">Query from application.</param>
        /// <param name="musicFolderId">If specified, only return artists in the music folder with the given ID.</param>
        /// <param name="ifModifiedSince">If specified, only return a result if the artist collection has changed since the given time (in milliseconds since 1 Jan 1970).</param>
        public async Task<OperationResult<subsonic.Response>> GetIndexes(subsonic.Request request, string musicFolderId = null, long? ifModifiedSince = null)
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
                foreach (var artistFirstLetter in (from c in this.DbContext.Artists
                                                   let first = c.Name.Substring(0, 1)
                                                   orderby first
                                                   select first).Distinct().ToArray())
                {
                    indexes.Add(new subsonic.Index
                    {
                        name = artistFirstLetter,
                        artist = (from c in this.DbContext.Artists
                                  where c.Name.Substring(0, 1) == artistFirstLetter
                                  where modifiedSinceFilter == null || c.LastUpdated >= modifiedSinceFilter
                                  orderby c.SortName, c.Name
                                  select new subsonic.Artist
                                  {
                                      id = subsonic.Request.ArtistIdIdentifier + c.RoadieId.ToString(),
                                      name = c.Name,
                                      artistImageUrl = this.MakeArtistThumbnailImage(c.RoadieId).Url,
                                      averageRating = 0,
                                      userRating = 0
                                  }).ToArray()
                    });
                }
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
                directory.starred = artistRating != null ? (DateTime?)(artistRating.LastUpdated ?? artistRating.CreatedDate) : null;
                directory.child = (from r in this.DbContext.Releases
                                   join ur in this.DbContext.UserReleases on r.Id equals ur.ReleaseId into urr
                                   from ur in urr.DefaultIfEmpty()
                                   let genre = r.Genres.FirstOrDefault()
                                   where r.ArtistId == artist.Id
                                   select new subsonic.Child
                                   {
                                       id = subsonic.Request.ReleaseIdIdentifier + r.RoadieId.ToString(),
                                       parent = subsonic.Request.ArtistIdIdentifier + artist.RoadieId.ToString(),
                                       isDir = true,
                                       title = r.Title,
                                       album = r.Title,
                                       albumId = subsonic.Request.ReleaseIdIdentifier + r.RoadieId.ToString(),
                                       artist = artist.Name,
                                       year = (r.ReleaseDate ?? r.CreatedDate).Year,
                                       genre = genre != null ? genre.Genre.Name : null,
                                       coverArt = subsonic.Request.ReleaseIdIdentifier + r.RoadieId.ToString(),
                                       averageRating = artist.Rating ?? 0,
                                       created = artist.CreatedDate,
                                       playCount = (from ut in this.DbContext.UserTracks
                                                          join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                                          join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                          where rm.ReleaseId == r.Id
                                                          where ut.PlayedCount != null
                                                          select ut.PlayedCount ?? 0).Sum(),
                                       starred = ur != null ? (ur.IsFavorite ?? false ? (DateTime?)(ur.LastUpdated ?? ur.CreatedDate) : null) : null,
                                   }).ToArray();
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
                directory.child = (from cr in this.DbContext.CollectionReleases
                                   join r in this.DbContext.Releases on cr.ReleaseId equals r.Id
                                   join a in this.DbContext.Artists on r.ArtistId equals a.Id
                                   join ur in this.DbContext.UserReleases on r.Id equals ur.ReleaseId into urr
                                   from ur in urr.DefaultIfEmpty()
                                   let genre = r.Genres.FirstOrDefault()
                                   let playCount = (from ut in this.DbContext.UserTracks
                                                    join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                                    join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                    where rm.ReleaseId == r.Id
                                                    select ut.PlayedCount).Sum()
                                   where cr.CollectionId == collection.Id
                                   select new subsonic.Child
                                   {
                                       id = subsonic.Request.ReleaseIdIdentifier + r.RoadieId.ToString(),
                                       parent = subsonic.Request.CollectionIdentifier + collection.RoadieId.ToString(),
                                       isDir = true,
                                       title = r.Title,
                                       album = r.Title,
                                       albumId = subsonic.Request.ReleaseIdIdentifier + r.RoadieId.ToString(),
                                       artist = a.Name,
                                       year = (r.ReleaseDate ?? r.CreatedDate).Year,
                                       genre = genre != null ? genre.Genre.Name : null,
                                       coverArt = subsonic.Request.ReleaseIdIdentifier + r.RoadieId.ToString(),
                                       created = collection.CreatedDate,
                                       playCount = playCount ?? 0
                                   }).ToArray();

            }
            // Request to get Tracks for an Album
            else
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
                directory.starred = releaseRating != null ? (releaseRating.IsFavorite ?? false ? (DateTime?)(releaseRating.LastUpdated ?? releaseRating.CreatedDate) : null) : null;
                var releaseTracks = release.Medias.SelectMany(x => x.Tracks);
                var genre = release.Genres.FirstOrDefault();
                directory.child = (from t in releaseTracks
                                   join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId into utg
                                   from ut in utg.DefaultIfEmpty()
                                   join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                   let playCount = (from ut in this.DbContext.UserTracks
                                                    where ut.TrackId == t.Id
                                                    select ut.PlayedCount).Sum()
                                   select new subsonic.Child
                                   {
                                       id = subsonic.Request.TrackIdIdentifier + t.RoadieId.ToString(),
                                       album = release.Title,
                                       albumId = subsonic.Request.ReleaseIdIdentifier + release.RoadieId.ToString(),
                                       artist = release.Artist.Name,
                                       artistId = subsonic.Request.ArtistIdIdentifier + release.Artist.RoadieId.ToString(),
                                       averageRating = release.Rating ?? 0,
                                       averageRatingSpecified= true,
                                       bitRate = 320,
                                       bitRateSpecified = true,
                                       contentType = "audio/mpeg",
                                       coverArt = subsonic.Request.ReleaseIdIdentifier + release.RoadieId.ToString(),
                                       created = release.CreatedDate,
                                       createdSpecified = true,
                                       discNumber = rm.MediaNumber,
                                       discNumberSpecified = true,
                                       duration = t.Duration.ToSecondsFromMilliseconds(),
                                       durationSpecified = true,
                                       genre = genre != null ? genre.Genre.Name : null,
                                       parent = subsonic.Request.ReleaseIdIdentifier + release.RoadieId.ToString(),
                                       path = $"{ release.Artist.Name}/{ release.Title}/{ t.TrackNumber } - { t.Title }.mp3",
                                       playCountSpecified = true,                                       
                                       size = t.FileSize ?? 0,
                                       sizeSpecified = true,
                                       starred = ut != null ? (ut.IsFavorite ?? false ? (DateTime?)(ut.LastUpdated ?? ut.CreatedDate) : null) : null,
                                       starredSpecified = ut != null,
                                       suffix = "mp3",
                                       title = t.Title,
                                       track = t.TrackNumber,
                                       trackSpecified = true,
                                       type = subsonic.MediaType.music,
                                       typeSpecified = true,
                                       userRating = ut != null ? ut.Rating : 0,
                                       userRatingSpecified = ut != null,
                                       year = (release.ReleaseDate ?? release.CreatedDate).Year,
                                       yearSpecified = true,
                                       playCount = (from ut in this.DbContext.UserTracks
                                                    join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                                    join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                    where rm.ReleaseId == release.Id
                                                    where ut.PlayedCount != null
                                                    select ut.PlayedCount ?? 0).Sum(),
                                   }).ToArray();
                directory.playCount = directory.child.Select(x => x.playCount).Sum();
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

        public async Task<FileOperationResult<Roadie.Library.Models.Image>> GetCoverArt(subsonic.Request request, int? size)
        {
            var sw = Stopwatch.StartNew();
            var result = new FileOperationResult<Roadie.Library.Models.Image>
            {
                Data = new Roadie.Library.Models.Image()
            };

            if (request.ArtistId != null)
            {
                var artist = this.GetArtist(request.ArtistId.Value);
                if (artist == null)
                {
                    return new FileOperationResult<Roadie.Library.Models.Image>(true, $"Invalid ArtistId [{ request.ArtistId}]");
                }
                result.Data.Bytes = artist.Thumbnail;
            }
            else if(request.CollectionId != null)
            {
                var collection = this.GetCollection(request.CollectionId.Value);
                if (collection == null)
                {
                    return new FileOperationResult<Roadie.Library.Models.Image>(true, $"Invalid CollectionId [{ request.CollectionId}]");
                }
                result.Data.Bytes = collection.Thumbnail;
            }
            else if(request.ReleaseId != null)
            {
                var release =  this.GetRelease(request.ReleaseId.Value);
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
            else if(!string.IsNullOrEmpty(request.u))
            {
                var userByUsername = this.DbContext.Users.FirstOrDefault(x => x.UserName == request.u);
                if(userByUsername == null)
                {
                    return new FileOperationResult<Roadie.Library.Models.Image>(true, $"Invalid Username [{ request.u}]");
                }
                result.Data.Bytes = userByUsername.Avatar;
            }

            if (size.HasValue && result.Data.Bytes != null)
            {
                result.Data.Bytes = ImageHelper.ResizeImage(result.Data.Bytes, size.Value, size.Value);
                result.ETag = EtagHelper.GenerateETag(this.HttpEncoder, result.Data.Bytes);
                result.LastModified = DateTime.UtcNow;
            }
            result.IsSuccess = true;
            sw.Stop();
            return new FileOperationResult<Roadie.Library.Models.Image>(result.Messages)
            {
                Data = result.Data,
                ETag = result.ETag,
                LastModified = result.LastModified,
                ContentType = "image/jpeg",
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

    }
}