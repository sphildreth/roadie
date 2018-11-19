using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
using subsonic = Roadie.Library.Models.ThirdPartyApi.Subsonic;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

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
        public const string ArtistIdIdentifier = "A:";
        public const string CollectionIdentifier = "C:";
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
            subsonic.MusicFolder musicFolderFilter = string.IsNullOrEmpty(musicFolderId) ? null : this.MusicFolders().FirstOrDefault(x => x.id == SafeParser.ToNumber<int>(musicFolderId));
            var indexes = new List<subsonic.Index>();

            if (musicFolderFilter == this.CollectionMusicFolder())
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
                                      id = SubsonicService.CollectionIdentifier + c.RoadieId.ToString(),
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
                                      id = SubsonicService.ArtistIdIdentifier + c.RoadieId.ToString(),
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
                                 owner = u.Username,                                 
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
            throw new NotImplementedException();
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

    }
}