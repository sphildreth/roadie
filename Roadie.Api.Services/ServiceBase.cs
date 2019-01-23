using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public abstract class ServiceBase
    {
        public static string TrackTokenSalt = "B0246908-FBD6-4E12-A96C-AF5B086115B3";

        protected readonly ICacheManager _cacheManager = null;
        protected readonly IRoadieSettings _configuration = null;
        protected readonly data.IRoadieDbContext _dbContext = null;
        protected readonly IHttpContext _httpContext = null;
        protected readonly IHttpEncoder _httpEncoder = null;
        protected readonly ILogger _logger = null;

        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected IRoadieSettings Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        protected data.IRoadieDbContext DbContext
        {
            get
            {
                return this._dbContext;
            }
        }

        protected IHttpContext HttpContext
        {
            get
            {
                return this._httpContext;
            }
        }

        protected IHttpEncoder HttpEncoder
        {
            get
            {
                return this._httpEncoder;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._logger;
            }
        }

        public ServiceBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, data.IRoadieDbContext context,
                             ICacheManager cacheManager, ILogger logger, IHttpContext httpContext)
        {
            this._configuration = configuration;
            this._httpEncoder = httpEncoder;
            this._dbContext = context;
            this._cacheManager = cacheManager;
            this._logger = logger;
            this._httpContext = httpContext;
        }

        public static bool ConfirmTrackPlayToken(ApplicationUser user, Guid trackRoadieId, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            return ServiceBase.TrackPlayToken(user, trackRoadieId).Equals(token);
        }

        public static string TrackPlayToken(ApplicationUser user, Guid trackId)
        {
            var hashids = new Hashids(ServiceBase.TrackTokenSalt);
            var trackIdPart = BitConverter.ToInt32(trackId.ToByteArray(), 6);
            if (trackIdPart < 0)
            {
                trackIdPart = trackIdPart * -1;
            }
            var token = hashids.Encode(user.Id, SafeParser.ToNumber<int>(user.CreatedDate.Value.ToString("DDHHmmss")), trackIdPart);
            return token;
        }

        public Image MakeThumbnailImage(Guid id, string type, int? width = null, int? height = null, bool includeCachebuster = false)
        {
            return this.MakeImage(id, type, width ?? this.Configuration.ThumbnailImageSize.Width, height ?? this.Configuration.ThumbnailImageSize.Height, null, includeCachebuster);
        }

        protected IEnumerable<int> ArtistIdsForRelease(int releaseId)
        {
            var trackArtistIds = (from r in this.DbContext.Releases
                                  join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                  join tr in this.DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                  where r.Id == releaseId
                                  where tr.ArtistId != null
                                  select tr.ArtistId.Value).ToList();
            trackArtistIds.Add(this.DbContext.Releases.FirstOrDefault(x => x.Id == releaseId).ArtistId);
            return trackArtistIds.Distinct().ToArray();
        }

        protected data.Artist GetArtist(string artistName)
        {
            if (string.IsNullOrEmpty(artistName))
            {
                return null;
            }
            var artistByName = this.CacheManager.Get(data.Artist.CacheUrnByName(artistName), () =>
            {
                return this.DbContext.Artists
                                    .FirstOrDefault(x => x.Name == artistName);
            }, null);
            if (artistByName == null)
            {
                return null;
            }
            return this.GetArtist(artistByName.RoadieId);
        }

        protected data.Artist GetArtist(Guid id)
        {
            return this.CacheManager.Get(data.Artist.CacheUrn(id), () =>
            {
                return this.DbContext.Artists
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Artist.CacheRegionUrn(id));
        }

        protected data.Collection GetCollection(Guid id)
        {
            return this.CacheManager.Get(data.Collection.CacheUrn(id), () =>
            {
                return this.DbContext.Collections
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Collection.CacheRegionUrn(id));
        }

        protected data.Label GetLabel(Guid id)
        {
            return this.CacheManager.Get(data.Label.CacheUrn(id), () =>
            {
                return this.DbContext.Labels
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Label.CacheRegionUrn(id));
        }

        protected data.Playlist GetPlaylist(Guid id)
        {
            return this.CacheManager.Get(data.Playlist.CacheUrn(id), () =>
            {
                return this.DbContext.Playlists
                                    .Include(x => x.User)
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Playlist.CacheRegionUrn(id));
        }

        protected data.Release GetRelease(Guid id)
        {
            return this.CacheManager.Get(data.Release.CacheUrn(id), () =>
            {
                return this.DbContext.Releases
                                    .Include(x => x.Artist)
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .Include(x => x.Medias)
                                    .Include("Medias.Tracks")
                                    .Include("Medias.Tracks.TrackArtist")
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Release.CacheRegionUrn(id));
        }

        /// <summary>
        /// Get Track by Subsonic Id ("T:guid")
        /// </summary>
        protected data.Track GetTrack(string id)
        {
            Guid trackId = Guid.Empty;
            if (Guid.TryParse(id, out trackId))
            {
                return this.GetTrack(trackId);
            }
            return null;
        }

        // Only read operations
        protected data.Track GetTrack(Guid id)
        {
            return this.CacheManager.Get(data.Track.CacheUrn(id), () =>
            {
                return this.DbContext.Tracks
                                     .Include(x => x.ReleaseMedia)
                                     .Include(x => x.ReleaseMedia.Release)
                                     .Include(x => x.ReleaseMedia.Release.Artist)
                                     .Include(x => x.TrackArtist)
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Track.CacheRegionUrn(id));
        }

        protected ApplicationUser GetUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }
            var userByUsername = this.CacheManager.Get(ApplicationUser.CacheUrnByUsername(username), () =>
            {
                return this.DbContext.Users
                                    .FirstOrDefault(x => x.UserName == username);
            }, null);
            return this.GetUser(userByUsername?.RoadieId);
        }

        protected ApplicationUser GetUser(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            return this.CacheManager.Get(ApplicationUser.CacheUrn(id.Value), () =>
            {
                return this.DbContext.Users
                                    .Include(x => x.ArtistRatings)
                                    .Include(x => x.ReleaseRatings)
                                    .Include(x => x.TrackRatings)
                                    .Include(x => x.UserRoles)
                                    .Include("UserRoles.Role")
                                    .Include("UserRoles.Role.RoleClaims")
                                    .Include(x => x.Claims)
                                    .Include(x => x.UserQues)
                                    .Include("UserQues.Track")
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, ApplicationUser.CacheRegionUrn(id.Value));
        }

        protected Image MakeArtistThumbnailImage(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            return MakeThumbnailImage(id.Value, "artist");
        }

        protected Image MakeCollectionThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "collection");
        }

        protected Image MakeFullsizeImage(Guid id, string caption = null)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/{id}", caption, $"{this.HttpContext.ImageBaseUrl }/{id}/{ this.Configuration.SmallImageSize.Width }/{ this.Configuration.SmallImageSize.Height }");
        }

        protected Image MakeImage(Guid id, int width = 200, int height = 200, string caption = null, bool includeCachebuster = false)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/{id}/{ width }/{ height }/{ (includeCachebuster ? DateTime.UtcNow.Ticks.ToString() : string.Empty) }", caption, $"{this.HttpContext.ImageBaseUrl }/{id}/{ this.Configuration.SmallImageSize.Width }/{ this.Configuration.SmallImageSize.Height }");
        }

        protected Image MakeImage(Guid id, string type, ImageSize imageSize)
        {
            return this.MakeImage(id, type, imageSize.Width, imageSize.Height);
        }

        protected Image MakeLabelThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "label");
        }

        protected string MakeLastFmUrl(string artistName, string releaseTitle)
        {
            return "http://www.last.fm/music/" + this.HttpEncoder.UrlEncode($"{ artistName }/{ releaseTitle }");
        }

        protected Image MakePlaylistThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "playlist");
        }

        protected Image MakeReleaseThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "release");
        }

        protected string MakeTrackPlayUrl(ApplicationUser user, int trackId, Guid trackRoadieId)
        {
            return $"{ this.HttpContext.BaseUrl }/play/track/{user.Id}/{ ServiceBase.TrackPlayToken(user, trackRoadieId)}/{ trackRoadieId }.mp3";
        }

        protected Image MakeTrackThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "track");
        }

        protected Image MakeUserThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "user");
        }

        protected async Task<OperationResult<short>> SetArtistRating(Guid artistId, ApplicationUser user, short rating)
        {
            var artist = this.DbContext.Artists
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                return new OperationResult<short>(true, $"Invalid Artist Id [{ artistId }]");
            }
            var now = DateTime.UtcNow;
            var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    Rating = rating,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                this.DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.Rating = rating;
                userArtist.LastUpdated = now;
            }
            await this.DbContext.SaveChangesAsync();

            var ratings = this.DbContext.UserArtists.Where(x => x.ArtistId == artist.Id && x.Rating > 0).Select(x => x.Rating);
            if (ratings != null && ratings.Any())
            {
                artist.Rating = (short)ratings.Average(x => (decimal)x);
            }
            else
            {
                artist.Rating = 0;
            }
            artist.LastUpdated = now;
            await this.DbContext.SaveChangesAsync();
            await this.UpdateArtistRank(artist.Id);
            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(artist.CacheRegion);

            artist = this.GetArtist(artistId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = artist.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetReleaseRating(Guid releaseId, ApplicationUser user, short rating)
        {
            var release = this.DbContext.Releases
                                    .Include(x => x.Artist)
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .Include(x => x.Medias)
                                    .Include("Medias.Tracks")
                                    .Include("Medias.Tracks.TrackArtist")
                                    .FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
            {
                return new OperationResult<short>(true, $"Invalid Release Id [{ releaseId }]");
            }
            var userRelease = this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            var now = DateTime.UtcNow;
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    Rating = rating,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                this.DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.Rating = rating;
                userRelease.LastUpdated = now;
            }
            await this.DbContext.SaveChangesAsync();

            var ratings = this.DbContext.UserReleases.Where(x => x.ReleaseId == release.Id && x.Rating > 0).Select(x => x.Rating);
            if (ratings != null && ratings.Any())
            {
                release.Rating = (short)ratings.Average(x => (decimal)x);
            }
            else
            {
                release.Rating = 0;
            }            
            release.LastUpdated = now;
            await this.DbContext.SaveChangesAsync();
            await this.UpdateReleaseRank(release.Id);
            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(release.CacheRegion);
            this.CacheManager.ClearRegion(release.Artist.CacheRegion);

            release = this.GetRelease(releaseId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = release.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetTrackRating(Guid trackId, ApplicationUser user, short rating)
        {
            var track = this.DbContext.Tracks
                                     .Include(x => x.ReleaseMedia)
                                     .Include(x => x.ReleaseMedia.Release)
                                     .Include(x => x.ReleaseMedia.Release.Artist)
                                     .Include(x => x.TrackArtist)
                                    .FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null)
            {
                return new OperationResult<short>(true, $"Invalid Track Id [{ trackId }]");
            }
            var now = DateTime.UtcNow;
            var userTrack = this.DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    Rating = rating,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                this.DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.Rating = rating;
                userTrack.LastUpdated = now;
            }
            await this.DbContext.SaveChangesAsync();

            var ratings = this.DbContext.UserTracks.Where(x => x.TrackId == track.Id && x.Rating > 0).Select(x => x.Rating);
            if (ratings != null && ratings.Any())
            {
                track.Rating = (short)ratings.Average(x => (decimal)x);
            } else
            {
                track.Rating = 0;
            }
            track.LastUpdated = now;
            await this.DbContext.SaveChangesAsync();
            await this.UpdateReleaseRank(track.ReleaseMedia.Release.Id);

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(track.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            track = this.GetTrack(trackId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = track.Rating
            };
        }

        protected async Task<OperationResult<bool>> ToggleArtistDisliked(Guid artistId, ApplicationUser user, bool isDisliked)
        {
            var artist = this.DbContext.Artists
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                return new OperationResult<bool>(true, $"Invalid Artist Id [{ artistId }]");
            }
            var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    IsDisliked = isDisliked,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                this.DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.IsDisliked = isDisliked;
                userArtist.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleArtistFavorite(Guid artistId, ApplicationUser user, bool isFavorite)
        {
            var artist = this.DbContext.Artists
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                return new OperationResult<bool>(true, $"Invalid Artist Id [{ artistId }]");
            }
            var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    IsFavorite = isFavorite,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                this.DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.IsFavorite = isFavorite;
                userArtist.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleReleaseDisliked(Guid releaseId, ApplicationUser user, bool isDisliked)
        {
            var release = this.DbContext.Releases
                                    .Include(x => x.Artist)
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .Include(x => x.Medias)
                                    .Include("Medias.Tracks")
                                    .Include("Medias.Tracks.TrackArtist")
                                    .FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
            {
                return new OperationResult<bool>(true, $"Invalid Release Id [{ releaseId }]");
            }
            var userRelease = this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    IsDisliked = isDisliked,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                this.DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.IsDisliked = isDisliked;
                userRelease.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(release.CacheRegion);
            this.CacheManager.ClearRegion(release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleReleaseFavorite(Guid releaseId, ApplicationUser user, bool isFavorite)
        {
            var release = this.DbContext.Releases
                                    .Include(x => x.Artist)
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .Include(x => x.Medias)
                                    .Include("Medias.Tracks")
                                    .Include("Medias.Tracks.TrackArtist")
                                    .FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
            {
                return new OperationResult<bool>(true, $"Invalid Release Id [{ releaseId }]");
            }
            var userRelease = this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    IsFavorite = isFavorite,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                this.DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.IsFavorite = isFavorite;
                userRelease.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(release.CacheRegion);
            this.CacheManager.ClearRegion(release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleTrackDisliked(Guid trackId, ApplicationUser user, bool isDisliked)
        {
            var track = this.DbContext.Tracks
                                     .Include(x => x.ReleaseMedia)
                                     .Include(x => x.ReleaseMedia.Release)
                                     .Include(x => x.ReleaseMedia.Release.Artist)
                                     .Include(x => x.TrackArtist)
                                    .FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Invalid Track Id [{ trackId }]");
            }
            var userTrack = this.DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    IsDisliked = isDisliked,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                this.DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.IsDisliked = isDisliked;
                userTrack.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(track.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleTrackFavorite(Guid trackId, ApplicationUser user, bool isFavorite)
        {
            var track = this.DbContext.Tracks
                                     .Include(x => x.ReleaseMedia)
                                     .Include(x => x.ReleaseMedia.Release)
                                     .Include(x => x.ReleaseMedia.Release.Artist)
                                     .Include(x => x.TrackArtist)
                                    .FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Invalid Track Id [{ trackId }]");
            }
            var userTrack = this.DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    IsFavorite = isFavorite,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                this.DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.IsFavorite = isFavorite;
                userTrack.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(track.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task UpdateArtistCounts(int artistId, DateTime now)
        {
            var artist = this.DbContext.Artists.FirstOrDefault(x => x.Id == artistId);
            if (artist != null)
            {
                artist.ReleaseCount = this.DbContext.Releases.Where(x => x.ArtistId == artistId).Count();
                artist.TrackCount = (from r in this.DbContext.Releases
                                     join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                     join tr in this.DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                     where (tr.ArtistId == artistId || r.ArtistId == artistId)
                                     select tr).Count();

                artist.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(artist.CacheRegion);
            }
        }

        /// <summary>
        /// Update the counts for all artists on a release (both track and release artists)
        /// </summary>
        protected async Task UpdateArtistCountsForRelease(int releaseId, DateTime now)
        {
            foreach (var artistId in this.ArtistIdsForRelease(releaseId))
            {
                await this.UpdateArtistCounts(artistId, now);
            }
        }

        protected async Task UpdateLabelCounts(int labelId, DateTime now)
        {
            var label = this.DbContext.Labels.FirstOrDefault(x => x.Id == labelId);
            if (label != null)
            {
                label.ReleaseCount = this.DbContext.ReleaseLabels.Where(x => x.LabelId == label.Id).Count();
                label.ArtistCount = (from r in this.DbContext.Releases
                                     join rl in this.DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                     join a in this.DbContext.Artists on r.ArtistId equals a.Id
                                     where rl.LabelId == label.Id
                                     group a by a.Id into artists
                                     select artists).Select(x => x.Key).Count();
                label.TrackCount = (from r in this.DbContext.Releases
                                    join rl in this.DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                    join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                    join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                    where rl.LabelId == label.Id
                                    select t).Count();
                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(label.CacheRegion);
            }
        }

        protected async Task UpdatePlaylistCounts(int playlistId, DateTime now)
        {
            var playlist = this.DbContext.Playlists.FirstOrDefault(x => x.Id == playlistId);
            if (playlist != null)
            {
                var playlistTracks = this.DbContext.PlaylistTracks
                                                   .Include(x => x.Track)
                                                   .Include("Track.ReleaseMedia")
                                                   .Where(x => x.PlayListId == playlist.Id).ToArray();
                playlist.TrackCount = (short)playlistTracks.Count();
                playlist.Duration = playlistTracks.Sum(x => x.Track.Duration);
                playlist.ReleaseCount = (short)playlistTracks.Select(x => x.Track.ReleaseMedia.ReleaseId).Distinct().Count();
                playlist.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(playlist.CacheRegion);
            }
        }

        protected async Task UpdateReleaseCounts(int releaseId, DateTime now)
        {
            var release = this.DbContext.Releases.FirstOrDefault(x => x.Id == releaseId);
            if (release != null)
            {
                release.PlayedCount = (from t in this.DbContext.Tracks
                                            join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                            where rm.ReleaseId == releaseId
                                            where t.PlayedCount.HasValue
                                            select t).Sum(x => x.PlayedCount);
                release.Duration = (from t in this.DbContext.Tracks
                                    join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                    where rm.ReleaseId == releaseId
                                    select t).Sum(x => x.Duration);
                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(release.CacheRegion);
            }
        }

        /// <summary>
        /// Find all artists involved with release and update their rank
        /// </summary>
        protected async Task UpdateArtistsRankForRelease(data.Release release)
        {
            if(release != null)
            {
                var artistsForRelease = new List<int>
                {
                    release.ArtistId
                };
                var trackArtistsForRelease = (from t in this.DbContext.Tracks
                                              join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                              where rm.ReleaseId == release.Id
                                              where t.ArtistId.HasValue
                                              select t.ArtistId.Value).ToArray();
                artistsForRelease.AddRange(trackArtistsForRelease);
                foreach(var artistId in artistsForRelease.Distinct())
                {
                    await this.UpdateArtistRank(artistId);
                }
            }
        }

        /// <summary>
        /// Update Artist Rank
        /// Artist Rank is a sum of the artists release ranks + artist tracks rating + artist user rating
        /// </summary>
        protected async Task UpdateArtistRank(int artistId, bool updateReleaseRanks = false)
        {
            var artist = this.DbContext.Artists.FirstOrDefault(x => x.Id == artistId);
            if (artist != null)
            {
                if(updateReleaseRanks)
                {
                    var artistReleaseIds = this.DbContext.Releases.Where(x => x.ArtistId == artistId).Select(x => x.Id).ToArray();
                    foreach(var artistReleaseId in artistReleaseIds)
                    {
                        await this.UpdateReleaseRank(artistReleaseId, false);
                    }
                }

                var artistTrackAverage = (from t in this.DbContext.Tracks
                                          join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                          join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                                          where t.ArtistId == artist.Id
                                          select (double?)ut.Rating).ToArray().Average(x => x) ?? 0;

                var artistReleaseRatingSum = (from r in this.DbContext.Releases
                                            join ur in this.DbContext.UserReleases on r.Id equals ur.ReleaseId
                                            where r.ArtistId == artist.Id
                                            select (double?)ur.Rating).ToArray().Average(x => x) ?? 0;

                var artistReleaseRankSum = (from r in this.DbContext.Releases
                                              where r.ArtistId == artist.Id
                                              select r.Rank).ToArray().Average(x => x) ?? 0;

                artist.Rank = SafeParser.ToNumber<decimal>(artistTrackAverage + artistReleaseRatingSum) + artistReleaseRankSum + artist.Rating;

                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(artist.CacheRegion);
                this.Logger.LogInformation("UpdatedArtistRank For Artist `{0}`", artist);
            }
        }

        /// <summary>
        /// Update Relase Rank
        /// Release Rank Calculation = Average of Track User Ratings + (User Rating of Release / Release Track Count) + Collection Rank Value
        /// </summary>
        protected async Task UpdateReleaseRank(int releaseId, bool updateArtistRank = true)
        {
            var release = this.DbContext.Releases.FirstOrDefault(x => x.Id == releaseId);
            if (release != null)
            {
                var releaseTrackAverage = (from t in this.DbContext.Tracks
                                        join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                        join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                                        where rm.ReleaseId == releaseId
                                        select (double?)ut.Rating).ToArray().Average(x => x);

                var releaseUserRatingRank = release.Rating > 0 ? (decimal?)release.Rating / (decimal?)release.TrackCount : 0;

                var collectionsWithRelease = (from c in this.DbContext.Collections
                                              join cr in this.DbContext.CollectionReleases on c.Id equals cr.CollectionId
                                              where c.CollectionType != Library.Enums.CollectionType.Chart
                                              where cr.ReleaseId == release.Id
                                              select new
                                              {
                                                  c.CollectionCount,
                                                  cr.ListNumber
                                              });

                decimal releaseCollectionRank = 0;
                foreach (var collectionWithRelease in collectionsWithRelease)
                {
                    var rank = (decimal)((collectionWithRelease.CollectionCount * .01) - ((collectionWithRelease.ListNumber - 1) * .01));
                    releaseCollectionRank += rank;
                }
                release.Rank = SafeParser.ToNumber<decimal>(releaseTrackAverage) + releaseUserRatingRank + releaseCollectionRank;

                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(release.CacheRegion);
                this.Logger.LogInformation("UpdateReleaseRank For Release `{0}`", release);
                if (updateArtistRank)
                {
                    await this.UpdateArtistsRankForRelease(release);
                }
            }
        }


        private Image MakeImage(Guid id, string type, int? width, int? height, string caption = null, bool includeCachebuster = false)
        {
            if (width.HasValue && height.HasValue && (width.Value != this.Configuration.ThumbnailImageSize.Width || height.Value != this.Configuration.ThumbnailImageSize.Height))
            {
                return new Image($"{this.HttpContext.ImageBaseUrl }/{type}/{id}/{width}/{height}/{ (includeCachebuster ? DateTime.UtcNow.Ticks.ToString() : string.Empty) }", caption, $"{this.HttpContext.ImageBaseUrl }/{type}/{id}/{ this.Configuration.ThumbnailImageSize.Width }/{ this.Configuration.ThumbnailImageSize.Height }");
            }
            return new Image($"{this.HttpContext.ImageBaseUrl }/{type}/{id}", caption, null);
        }
    }
}