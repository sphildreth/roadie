using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.FileName;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.MetaData.LastFm;
using mb = Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;
using Roadie.Library.Processors;

namespace Roadie.Api.Services
{

    public class ArtistService : ServiceBase, IArtistService
    {
        private ICollectionService CollectionService { get; } = null;
        private IPlaylistService PlaylistService { get; } = null;
        private IBookmarkService BookmarkService { get; } = null;

        private IArtistLookupEngine ArtistLookupEngine { get; }
        private IAudioMetaDataHelper AudioMetaDataHelper { get; }
        private IFileNameHelper FileNameHelper { get; }
        private IID3TagsHelper ID3TagsHelper { get; }
        private IImageFactory ImageFactory { get; }
        private ILabelFactory LabelFactory { get; }
        private ILabelLookupEngine LabelLookupEngine { get; }
        private ILastFmHelper LastFmHelper { get; }
        private mb.IMusicBrainzProvider MusicBrainzProvider { get; }
        private IReleaseFactory ReleaseFactory { get; }
        private IArtistFactory ArtistFactory { get; }
        private IReleaseLookupEngine ReleaseLookupEngine { get; }

        public ArtistService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService,
                             IBookmarkService bookmarkService
            )
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            this.CollectionService = collectionService;
            this.PlaylistService = playlistService;
            this.BookmarkService = bookmarkService;

            this.MusicBrainzProvider = new mb.MusicBrainzProvider(configuration, cacheManager, logger);
            this.LastFmHelper = new LastFmHelper(configuration, cacheManager, logger);
            this.FileNameHelper = new FileNameHelper(configuration, cacheManager, logger);
            this.ID3TagsHelper = new ID3TagsHelper(configuration, cacheManager, logger);
            this.ArtistLookupEngine = new ArtistLookupEngine(configuration, httpEncoder, dbContext, cacheManager, logger);
            this.LabelLookupEngine = new LabelLookupEngine(configuration, httpEncoder, dbContext, cacheManager, logger);
            this.ReleaseLookupEngine = new ReleaseLookupEngine(configuration, httpEncoder, dbContext, cacheManager, logger, this.ArtistLookupEngine, this.LabelLookupEngine);
            this.ImageFactory = new ImageFactory(configuration, httpEncoder, dbContext, cacheManager, logger, this.ArtistLookupEngine, this.ReleaseLookupEngine);
            this.LabelFactory = new LabelFactory(configuration, httpEncoder, dbContext, cacheManager, logger, this.ArtistLookupEngine, this.ReleaseLookupEngine);
            this.AudioMetaDataHelper = new AudioMetaDataHelper(configuration, httpEncoder, dbContext, this.MusicBrainzProvider, this.LastFmHelper, cacheManager,
                                                               logger, this.ArtistLookupEngine, this.ImageFactory, this.FileNameHelper, this.ID3TagsHelper);

            this.ReleaseFactory = new ReleaseFactory(configuration, httpEncoder, dbContext, cacheManager, logger, this.ArtistLookupEngine, this.LabelFactory, this.AudioMetaDataHelper, this.ReleaseLookupEngine);
            this.ArtistFactory = new ArtistFactory(configuration, httpEncoder, dbContext, cacheManager, logger, this.ArtistLookupEngine, this.ReleaseFactory, this.ImageFactory, this.ReleaseLookupEngine, this.AudioMetaDataHelper);
        }

        public async Task<OperationResult<Artist>> ById(User roadieUser, Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:artist_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Artist>>(cacheKey, async () =>
            {
                tsw.Restart();
                var rr = await this.ArtistByIdAction(id, includes);
                tsw.Stop();
                timings.Add("ArtistByIdAction", tsw.ElapsedMilliseconds);
                return rr;

            }, data.Artist.CacheRegionUrn(id));
            if (result?.Data != null && roadieUser != null)
            {
                tsw.Restart();
                var artist = this.GetArtist(id);
                tsw.Stop();
                timings.Add("GetArtist", tsw.ElapsedMilliseconds);
                tsw.Restart();
                var userBookmarkResult = await this.BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Artist);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Value == artist.RoadieId.ToString()) != null;
                }
                tsw.Stop();
                timings.Add("userBookmarkResult", tsw.ElapsedMilliseconds);
                tsw.Restart();
                var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == roadieUser.Id);
                if (userArtist != null)
                {
                    result.Data.UserRating = new UserArtist
                    {
                        IsDisliked = userArtist.IsDisliked ?? false,
                        IsFavorite = userArtist.IsFavorite ?? false,
                        Rating = userArtist.Rating
                    };
                }
                tsw.Stop();
                timings.Add("userArtist", tsw.ElapsedMilliseconds);
            }
            sw.Stop();
            timings.Add("operation", sw.ElapsedMilliseconds);
            this.Logger.LogDebug("ById Timings: id [{0}], includes [{1}], timings [{3}]", id, includes, JsonConvert.SerializeObject(timings));
            return new OperationResult<Artist>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Artist>> ArtistByIdAction(Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var artist = this.GetArtist(id);
            tsw.Stop();
            timings.Add("getArtist", tsw.ElapsedMilliseconds);

            if (artist == null)
            {
                return new OperationResult<Artist>(true, string.Format("Artist Not Found [{0}]", id));
            }
            tsw.Restart();
            var result = artist.Adapt<Artist>();
            result.BeginDate = result.BeginDate == null || result.BeginDate == DateTime.MinValue ? null : result.BeginDate;
            result.EndDate = result.EndDate == null || result.EndDate == DateTime.MinValue ? null : result.EndDate;
            result.BirthDate = result.BirthDate == null || result.BirthDate == DateTime.MinValue ? null : result.BirthDate;
            tsw.Stop();
            timings.Add("adaptArtist", tsw.ElapsedMilliseconds);
            result.Thumbnail = base.MakeArtistThumbnailImage(id);
            result.MediumThumbnail = base.MakeThumbnailImage(id, "artist", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            tsw.Restart();
            result.Genres = artist.Genres.Select(x => new DataToken { Text = x.Genre.Name, Value = x.Genre.RoadieId.ToString() });
            tsw.Stop();
            timings.Add("genres", tsw.ElapsedMilliseconds);

            if (includes != null && includes.Any())
            {
                if (includes.Contains("releases"))
                {
                    var dtoReleases = new List<ReleaseList>();
                    foreach (var release in this.DbContext.Releases.Include("Medias").Include("Medias.Tracks").Include("Medias.Tracks").Where(x => x.ArtistId == artist.Id).ToArray())
                    {
                        var releaseList = release.Adapt<ReleaseList>();
                        releaseList.Thumbnail = base.MakeReleaseThumbnailImage(release.RoadieId);
                        var dtoReleaseMedia = new List<ReleaseMediaList>();
                        if (includes.Contains("tracks"))
                        {
                            foreach (var releasemedia in release.Medias.OrderBy(x => x.MediaNumber).ToArray())
                            {
                                var dtoMedia = releasemedia.Adapt<ReleaseMediaList>();
                                var tracks = new List<TrackList>();
                                foreach (var t in this.DbContext.Tracks.Where(x => x.ReleaseMediaId == releasemedia.Id).OrderBy(x => x.TrackNumber).ToArray())
                                {
                                    var track = t.Adapt<TrackList>();
                                    ArtistList trackArtist = null;
                                    if (t.ArtistId.HasValue)
                                    {
                                        var ta = this.DbContext.Artists.FirstOrDefault(x => x.Id == t.ArtistId.Value);
                                        if (ta != null)
                                        {
                                            trackArtist = ArtistList.FromDataArtist(ta, this.MakeArtistThumbnailImage(ta.RoadieId));
                                        }
                                    }
                                    track.TrackArtist = trackArtist;
                                    tracks.Add(track);
                                }
                                dtoMedia.Tracks = tracks;
                                dtoReleaseMedia.Add(dtoMedia);
                            }
                        }
                        releaseList.Media = dtoReleaseMedia;
                        dtoReleases.Add(releaseList);
                    }
                    result.Releases = dtoReleases;
                }
                if (includes.Contains("stats"))
                {
                    tsw.Restart();

                    // TODO this should be on artist properties to speed up fetch times

                    var artistTracks = (from r in this.DbContext.Releases
                                        join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                        join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                        where r.ArtistId == artist.Id
                                        select new
                                        {
                                            t.Id,
                                            size = t.FileSize,
                                            time = t.Duration,
                                            isMissing = t.Hash == null
                                        });
                    var validCartistTracks = artistTracks.Where(x => !x.isMissing);
                    var trackTime = validCartistTracks.Sum(x => x.time);
                    result.Statistics = new CollectionStatistics
                    {
                        FileSize = artistTracks.Sum(x => (long?)x.size).ToFileSize(),
                        MissingTrackCount = artistTracks.Where(x => x.isMissing).Count(),
                        ReleaseCount = artist.ReleaseCount,
                        ReleaseMediaCount = (from r in this.DbContext.Releases
                                             join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                             where r.ArtistId == artist.Id
                                             select rm.Id).Count(),
                        TrackTime = validCartistTracks.Any() ? new TimeInfo((decimal)trackTime).ToFullFormattedString() : "--:--",
                        TrackCount = validCartistTracks.Count(),
                        TrackPlayedCount = artist.PlayedCount
                    };
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("images"))
                {
                    tsw.Restart();
                    result.Images = this.DbContext.Images.Where(x => x.ArtistId == artist.Id).Select(x => MakeFullsizeImage(x.RoadieId, x.Caption)).ToArray();
                    tsw.Stop();
                    timings.Add("images", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("associatedartists"))
                {
                    tsw.Restart();
                    var associatedWithArtists = (from aa in this.DbContext.ArtistAssociations
                                                    join a in this.DbContext.Artists on aa.AssociatedArtistId equals a.Id
                                                    where aa.ArtistId == artist.Id
                                                     select new ArtistList
                                                     {
                                                         DatabaseId = a.Id,
                                                         Id = a.RoadieId,
                                                         Artist = new DataToken
                                                         {
                                                             Text = a.Name,
                                                             Value = a.RoadieId.ToString()
                                                         },
                                                         Thumbnail = this.MakeArtistThumbnailImage(a.RoadieId),
                                                         Rating = a.Rating,
                                                         CreatedDate = a.CreatedDate,
                                                         LastUpdated = a.LastUpdated,
                                                         LastPlayed = a.LastPlayed,
                                                         PlayedCount = a.PlayedCount,
                                                         ReleaseCount = a.ReleaseCount,
                                                         TrackCount = a.TrackCount,
                                                         SortName = a.SortName
                                                     }).ToArray();

                    var associatedArtists = (from aa in this.DbContext.ArtistAssociations
                                             join a in this.DbContext.Artists on aa.ArtistId equals a.Id
                                             where aa.AssociatedArtistId == artist.Id
                                             select new ArtistList
                                             {
                                                 DatabaseId = a.Id,
                                                 Id = a.RoadieId,
                                                 Artist = new DataToken
                                                 {
                                                     Text = a.Name,
                                                     Value = a.RoadieId.ToString()
                                                 },
                                                 Thumbnail = this.MakeArtistThumbnailImage(a.RoadieId),
                                                 Rating = a.Rating,
                                                 CreatedDate = a.CreatedDate,
                                                 LastUpdated = a.LastUpdated,
                                                 LastPlayed = a.LastPlayed,
                                                 PlayedCount = a.PlayedCount,
                                                 ReleaseCount = a.ReleaseCount,
                                                 TrackCount = a.TrackCount,
                                                 SortName = a.SortName
                                             }).ToArray();

                    result.AssociatedArtists = associatedArtists.Union(associatedWithArtists).OrderBy(x => x.SortName);
                    result.AssociatedArtistsTokens = result.AssociatedArtists.Select(x => x.Artist).ToArray();
                    tsw.Stop();
                    timings.Add("associatedartists", tsw.ElapsedMilliseconds);

                }
                if (includes.Contains("collections"))
                {
                    tsw.Restart();
                    var collectionPagedRequest = new PagedRequest
                    {
                        Limit = 100                        
                    };
                    var r = await this.CollectionService.List(roadieUser: null,
                                                              request: collectionPagedRequest, artistId: artist.RoadieId);
                    if (r.IsSuccess)
                    {
                        result.CollectionsWithArtistReleases = r.Rows.ToArray();
                    }
                    tsw.Stop();
                    timings.Add("collections", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("playlists"))
                {
                    tsw.Restart();
                    var pg = new PagedRequest
                    {
                        FilterToArtistId = artist.RoadieId
                    };
                    var r = await this.PlaylistService.List(pg);
                    if (r.IsSuccess)
                    {
                        result.PlaylistsWithArtistReleases = r.Rows.ToArray();
                    }
                    tsw.Stop();
                    timings.Add("playlists", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("contributions"))
                {
                    tsw.Restart();
                    result.ArtistContributionReleases = (from t in this.DbContext.Tracks
                                                         join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                         join r in this.DbContext.Releases.Include(x => x.Artist) on rm.ReleaseId equals r.Id
                                                         where t.ArtistId == artist.Id
                                                         group r by r.Id into rr
                                                         select rr)
                                                         .ToArray()
                                                         .Select(rr => rr.First())
                                                         .Select(r => ReleaseList.FromDataRelease(r, r.Artist, this.HttpContext.BaseUrl, MakeArtistThumbnailImage(r.Artist.RoadieId), MakeReleaseThumbnailImage(r.RoadieId)))
                                                         .ToArray().OrderBy(x => x.Release.Text).ToArray();
                    result.ArtistContributionReleases = result.ArtistContributionReleases.Any() ? result.ArtistContributionReleases : null;
                    tsw.Stop();
                    timings.Add("contributions", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("labels"))
                {
                    tsw.Restart();                   
                    result.ArtistLabels = (from l in this.DbContext.Labels
                                           join rl in this.DbContext.ReleaseLabels on l.Id equals rl.LabelId
                                           join r in this.DbContext.Releases on rl.ReleaseId equals r.Id
                                           where r.ArtistId == artist.Id
                                           orderby l.SortName
                                           select LabelList.FromDataLabel(l, this.MakeLabelThumbnailImage(l.RoadieId)))
                                           .ToArray()
                                           .GroupBy(x => x.Label.Value).Select(x => x.First()).OrderBy(x => x.SortName).ThenBy(x => x.Label.Text).ToArray();
                    result.ArtistLabels = result.ArtistLabels.Any() ? result.ArtistLabels : null;
                    tsw.Stop();
                    timings.Add("labels", tsw.ElapsedMilliseconds);
                }
            }
            sw.Stop();
            timings.Add("operation", sw.ElapsedMilliseconds);
            this.Logger.LogDebug("ArtistByIdAction Timings: id [{0}], includes [{1}], timings [{3}]", id, includes, JsonConvert.SerializeObject(timings));

            return new OperationResult<Artist>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public Task<Library.Models.Pagination.PagedResult<ArtistList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, bool? onlyIncludeWithReleases = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            int[] favoriteArtistIds = new int[0];
            if(request.FilterFavoriteOnly)
            {
                favoriteArtistIds = (from a in this.DbContext.Artists
                                     join ua in this.DbContext.UserArtists on a.Id equals ua.ArtistId
                                     where ua.IsFavorite ?? false
                                     where (roadieUser == null || ua.UserId == roadieUser.Id)
                                     select a.Id
                                     ).ToArray();
            }
            int[] labelArtistIds = new int[0];
            if(request.FilterToLabelId.HasValue)
            {
                labelArtistIds = (from l in this.DbContext.Labels
                                  join rl in this.DbContext.ReleaseLabels on l.Id equals rl.LabelId
                                  join r in this.DbContext.Releases on rl.ReleaseId equals r.Id
                                  where l.RoadieId == request.FilterToLabelId
                                  select r.ArtistId)
                                  .Distinct()
                                  .ToArray();
            }
            var onlyWithReleases = onlyIncludeWithReleases ?? true;
            var result = (from a in this.DbContext.Artists
                          where (!onlyWithReleases || a.ReleaseCount > 0)
                          where (request.FilterToArtistId == null || a.RoadieId == request.FilterToArtistId)
                          where (request.FilterMinimumRating == null || a.Rating >= request.FilterMinimumRating.Value)
                          where (request.FilterValue == "" || (a.Name.Contains(request.FilterValue) || a.SortName.Contains(request.FilterValue) || a.AlternateNames.Contains(request.FilterValue)))
                          where (!request.FilterFavoriteOnly || favoriteArtistIds.Contains(a.Id))
                          where (request.FilterToLabelId == null || labelArtistIds.Contains(a.Id))
                          select new ArtistList
                          {
                              DatabaseId = a.Id,
                              Id = a.RoadieId,
                              Artist = new DataToken
                              {
                                  Text = a.Name,
                                  Value = a.RoadieId.ToString()
                              },
                              Thumbnail = this.MakeArtistThumbnailImage(a.RoadieId),
                              Rating = a.Rating,
                              CreatedDate = a.CreatedDate,
                              LastUpdated = a.LastUpdated,
                              LastPlayed = a.LastPlayed,
                              PlayedCount = a.PlayedCount,
                              ReleaseCount = a.ReleaseCount,
                              TrackCount = a.TrackCount,
                              SortName = a.SortName
                          }).Distinct();

            ArtistList[] rows = null;
            var rowCount = result.Count();
            if (doRandomize ?? false)
            {

                var randomLimit = roadieUser?.RandomReleaseLimit ?? 100;
                request.Limit = request.LimitValue > randomLimit ? randomLimit : request.LimitValue;
                rows = result.OrderBy(x => Guid.NewGuid()).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            else
            {
                string sortBy = "Id";
                if (request.ActionValue == User.ActionKeyUserRated)
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Rating", "DESC" }, { "Artist.Text", "ASC" } }) : request.OrderValue(null);
                }
                else
                {
                    sortBy = request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Artist.Text", "ASC" } });
                }
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            if (rows.Any() && roadieUser != null)
            {
                var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                var userArtistRatings = (from ua in this.DbContext.UserArtists
                                          where ua.UserId == roadieUser.Id
                                          where rowIds.Contains(ua.ArtistId)
                                          select ua).ToArray();

                foreach (var userArtistRating in userArtistRatings.Where(x => rows.Select(r => r.DatabaseId).Contains(x.ArtistId)))
                {
                    var row = rows.FirstOrDefault(x => x.DatabaseId == userArtistRating.ArtistId);
                    if (row != null)
                    {
                        row.UserRating = new UserArtist
                        {
                            IsDisliked = userArtistRating.IsDisliked ?? false,
                            IsFavorite = userArtistRating.IsFavorite ?? false,
                            Rating = userArtistRating.Rating,
                            RatedDate = userArtistRating.LastUpdated ?? userArtistRating.CreatedDate
                        };
                    }
                }
            }
            sw.Stop();
            return Task.FromResult(new Library.Models.Pagination.PagedResult<ArtistList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        public async Task<OperationResult<Library.Models.Image>> SetReleaseImageByUrl(User user, Guid id, string imageUrl)
        {
            return await this.SaveImageBytes(user, id, WebHelper.BytesForImageUrl(imageUrl));
        }

        public async Task<OperationResult<Library.Models.Image>> UploadArtistImage(User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }
            return await this.SaveImageBytes(user, id, bytes);
        }

        public async Task<OperationResult<bool>> UpdateArtist(User user, Artist model)
        {
            var didRenameArtist = false;
            var didChangeThumbnail = false;
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = this.DbContext.Artists
                                        .Include(x => x.Genres)
                                        .Include("Genres.Genre")
                                        .FirstOrDefault(x => x.RoadieId == model.Id);
            if (artist == null)
            {
                return new OperationResult<bool>(true, string.Format("Artist Not Found [{0}]", model.Id));
            }
            try
            {
                var now = DateTime.UtcNow;
                var originalArtistFolder = artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                artist.AlternateNames = model.AlternateNamesList.ToDelimitedList();
                artist.AmgId = model.AmgId;
                artist.BeginDate = model.BeginDate;
                artist.BioContext = model.BioContext;
                artist.BirthDate = model.BirthDate;
                artist.DiscogsId = model.DiscogsId;
                artist.EndDate = model.EndDate;
                artist.IsLocked = model.IsLocked;
                artist.ISNI = model.ISNIList.ToDelimitedList();
                artist.ITunesId = model.ITunesId;
                artist.MusicBrainzId = model.MusicBrainzId;
                artist.Name = model.Name;
                artist.Profile = model.Profile;
                artist.Rating = model.Rating;
                artist.RealName = model.RealName;
                artist.SortName = model.SortName;
                artist.SpotifyId = model.SpotifyId;
                artist.Status = SafeParser.ToEnum<Statuses>(model.Status);
                artist.Tags = model.TagsList.ToDelimitedList();
                artist.URLs = model.URLsList.ToDelimitedList();

                var newArtistFolder = artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                if (!newArtistFolder.Equals(originalArtistFolder, StringComparison.OrdinalIgnoreCase))
                {
                    didRenameArtist = true;

                    // Rename artist folder to reflect new artist name
                    this.Logger.LogTrace("Moving Artist From Folder [{0}] To  [{1}]", originalArtistFolder, newArtistFolder);
                    Directory.Move(originalArtistFolder, newArtistFolder);
                }
                var artistImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (artistImage != null)
                {
                    // Ensure is jpeg first
                    artist.Thumbnail = ImageHelper.ConvertToJpegFormat(artistImage);

                    // Save unaltered image to cover file
                    var artistImageName = Path.Combine(artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder), "artist.jpg");
                    File.WriteAllBytes(artistImageName, artist.Thumbnail);

                    // Resize to store in database as thumbnail
                    artist.Thumbnail = ImageHelper.ResizeImage(artist.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                    didChangeThumbnail = true;
                }

                if (model.Genres != null && model.Genres.Any())
                {
                    // Remove existing Genres not in model list
                    foreach (var genre in artist.Genres.ToList())
                    {
                        var doesExistInModel = model.Genres.Any(x => SafeParser.ToGuid(x.Value) == genre.Genre.RoadieId);
                        if (!doesExistInModel)
                        {
                            artist.Genres.Remove(genre);
                        }
                    }

                    // Add new Genres in model not in data
                    foreach (var genre in model.Genres)
                    {
                        var genreId = SafeParser.ToGuid(genre.Value);
                        var doesExistInData = artist.Genres.Any(x => x.Genre.RoadieId == genreId);
                        if (!doesExistInData)
                        {
                            var g = this.DbContext.Genres.FirstOrDefault(x => x.RoadieId == genreId);
                            if (g != null)
                            {
                                artist.Genres.Add(new data.ArtistGenre
                                {
                                    ArtistId = artist.Id,
                                    GenreId = g.Id,
                                    Genre = g
                                });
                            }
                        }
                    }
                }
                else if (model.Genres == null || !model.Genres.Any())
                {
                    artist.Genres.Clear();
                }

                if (model.AssociatedArtistsTokens != null && model.AssociatedArtistsTokens.Any())
                {
                    var associatedArtists = this.DbContext.ArtistAssociations.Include(x => x.AssociatedArtist).Where(x => x.ArtistId == artist.Id).ToList();

                    // Remove existing AssociatedArtists not in model list
                    foreach (var associatedArtist in associatedArtists)
                    {
                        var doesExistInModel = model.AssociatedArtistsTokens.Any(x => SafeParser.ToGuid(x.Value) == associatedArtist.AssociatedArtist.RoadieId);
                        if (!doesExistInModel)
                        {
                            this.DbContext.ArtistAssociations.Remove(associatedArtist);
                        }
                    }

                    // Add new AssociatedArtists in model not in data
                    foreach (var associatedArtist in model.AssociatedArtistsTokens)
                    {
                        var associatedArtistId = SafeParser.ToGuid(associatedArtist.Value);
                        var doesExistInData = associatedArtists.Any(x => x.AssociatedArtist.RoadieId == associatedArtistId);
                        if (!doesExistInData)
                        {
                            var a = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == associatedArtistId);
                            if (a != null)
                            {
                                this.DbContext.ArtistAssociations.Add(new data.ArtistAssociation
                                {
                                    ArtistId = artist.Id,
                                    AssociatedArtistId = a.Id
                                });
                            }
                        }
                    }
                }
                else if (model.AssociatedArtistsTokens == null || !model.AssociatedArtistsTokens.Any())
                {
                    artist.AssociatedArtists.Clear();
                }


                if (model.Images != null && model.Images.Any())
                {
                    // TODO
                }

                artist.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();
                if (didRenameArtist) {
                    // Update artist tracks to have new artist name in ID3 metadata
                    foreach (var mp3 in Directory.GetFiles(newArtistFolder, "*.mp3", SearchOption.AllDirectories))
                    {
                        var trackFileInfo = new FileInfo(mp3);
                        var audioMetaData = await this.AudioMetaDataHelper.GetInfo(trackFileInfo);
                        if (audioMetaData != null)
                        {
                            audioMetaData.Artist = artist.Name;
                            this.AudioMetaDataHelper.WriteTags(audioMetaData, trackFileInfo);
                        }
                    }
                    await this.ScanArtistReleasesFolders(artist.RoadieId, this.Configuration.LibraryFolder, false);
                }
                this.CacheManager.ClearRegion(artist.CacheRegion);
                this.Logger.LogInformation($"UpdateArtist `{ artist }` By User `{ user }`: Renamed Artist [{ didRenameArtist }], Uploaded new image [{ didChangeThumbnail }]");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
                errors.Add(ex);
            }
            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanArtistReleasesFolders(Guid artistId, string destinationFolder, bool doJustInfo)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(artistId != Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var artist = this.DbContext.Artists
                                           .Include("Releases")
                                           .Include("Releases.Labels")
                                           .FirstOrDefault(x => x.RoadieId == artistId);
                if (artist == null)
                {
                    this.Logger.LogWarning("Unable To Find Artist [{0}]", artistId);
                    return new OperationResult<bool>();
                }
                var releaseScannedCount = 0;
                var artistFolder = artist.ArtistFileFolder(this.Configuration, destinationFolder);
                var scannedArtistFolders = new List<string>();
                // Scan known releases for changes
                if (artist.Releases != null)
                {
                    foreach (var release in artist.Releases)
                    {
                        try
                        {
                            result = result && (await this.ReleaseFactory.ScanReleaseFolder(Guid.Empty, destinationFolder, doJustInfo, release)).Data;
                            releaseScannedCount++;
                            scannedArtistFolders.Add(release.ReleaseFileFolder(artistFolder));
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, ex.Serialize());
                        }
                    }
                }
                // Any folder found in Artist folder not already scanned scan
                var folderProcessor = new FolderProcessor(this.Configuration, this.HttpEncoder, destinationFolder, this.DbContext, this.CacheManager, this.Logger, this.ArtistLookupEngine, this.ArtistFactory, this.ReleaseFactory, this.ImageFactory, this.ReleaseLookupEngine, this.AudioMetaDataHelper);
                var nonReleaseFolders = (from d in Directory.EnumerateDirectories(artistFolder)
                                         where !(from r in scannedArtistFolders select r).Contains(d)
                                         orderby d
                                         select d);
                foreach (var folder in nonReleaseFolders)
                {
                    await folderProcessor.Process(new DirectoryInfo(folder), doJustInfo);
                }
                if (!doJustInfo)
                {
                    FolderProcessor.DeleteEmptyFolders(new DirectoryInfo(artistFolder), this.Logger);
                }

                // Always update artist image if artist image is found on an artist rescan
                var imageFiles = ImageHelper.ImageFilesInFolder(artistFolder);
                if (imageFiles != null && imageFiles.Any())
                {
                    var imageFile = imageFiles.First();
                    var i = new FileInfo(imageFile);
                    var iName = i.Name.ToLower().Trim();
                    var isArtistImage = iName.Contains("artist") || iName.Contains(artist.Name.ToLower());
                    if (isArtistImage)
                    {
                        // Read image and convert to jpeg
                        artist.Thumbnail = File.ReadAllBytes(i.FullName);
                        artist.Thumbnail = ImageHelper.ResizeImage(artist.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                        artist.Thumbnail = ImageHelper.ConvertToJpegFormat(artist.Thumbnail);
                        artist.LastUpdated = DateTime.UtcNow;
                        await this.DbContext.SaveChangesAsync();
                        this.CacheManager.ClearRegion(artist.CacheRegion);
                        this.Logger.LogInformation("Update Thumbnail using Artist File [{0}]", iName);
                    }
                }

                sw.Stop();
                this.CacheManager.ClearRegion(artist.CacheRegion);
                this.Logger.LogInformation("Scanned Artist [{0}], Releases Scanned [{1}], OperationTime [{2}]", artist.ToString(), releaseScannedCount, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
                resultErrors.Add(ex);
            }
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = resultErrors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }


        private async Task<OperationResult<Library.Models.Image>> SaveImageBytes(User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == id);
            if (artist == null)
            {
                return new OperationResult<Library.Models.Image>(true, string.Format("Artist Not Found [{0}]", id));
            }
            try
            {
                var now = DateTime.UtcNow;
                artist.Thumbnail = imageBytes;
                if (artist.Thumbnail != null)
                {
                    // Ensure is jpeg first
                    artist.Thumbnail = ImageHelper.ConvertToJpegFormat(artist.Thumbnail);

                    // Ensure artist folder exists
                    var artistFolder = artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                    if(!Directory.Exists(artistFolder))
                    {
                        Directory.CreateDirectory(artistFolder);
                        this.Logger.LogInformation("Created Artist Folder [0] for `artist`", artistFolder, artist);
                    }                    

                    // Save unaltered image to artist file
                    var coverFileName = Path.Combine(artistFolder, "artist.jpg");
                    File.WriteAllBytes(coverFileName, artist.Thumbnail);

                    // Resize to store in database as thumbnail
                    artist.Thumbnail = ImageHelper.ResizeImage(artist.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                }
                artist.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(artist.CacheRegion);
                this.Logger.LogInformation($"SaveImageBytes `{ artist }` By User `{ user }`");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
                errors.Add(ex);
            }
            sw.Stop();

            return new OperationResult<Library.Models.Image>
            {
                IsSuccess = !errors.Any(),
                Data = base.MakeThumbnailImage(id, "artist", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }
    }
}