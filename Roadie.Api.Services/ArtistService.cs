using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
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

namespace Roadie.Api.Services
{
    public class ArtistService : ServiceBase, IArtistService
    {
        private IArtistLookupEngine ArtistLookupEngine { get; }

        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        private IBookmarkService BookmarkService { get; }

        private ICollectionService CollectionService { get; }

        private IFileDirectoryProcessorService FileDirectoryProcessorService { get; }

        private IPlaylistService PlaylistService { get; }

        private IReleaseService ReleaseService { get; }

        public ArtistService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<ArtistService> logger,
            ICollectionService collectionService,
            IPlaylistService playlistService,
            IBookmarkService bookmarkService,
            IReleaseService releaseService,
            IArtistLookupEngine artistLookupEngine,
            IAudioMetaDataHelper audioMetaDataHelper,
            IFileDirectoryProcessorService fileDirectoryProcessorService
        )
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            CollectionService = collectionService;
            PlaylistService = playlistService;
            BookmarkService = bookmarkService;
            ArtistLookupEngine = artistLookupEngine;
            AudioMetaDataHelper = audioMetaDataHelper;
            ReleaseService = releaseService;
            FileDirectoryProcessorService = fileDirectoryProcessorService;
        }

        public async Task<OperationResult<Artist>> ById(Library.Models.Users.User roadieUser, Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = $"urn:artist_by_id_operation:{id}:{(includes == null ? "0" : string.Join("|", includes))}";
            var result = await CacheManager.GetAsync(cacheKey, async () =>
            {
                tsw.Restart();
                var rr = await ArtistByIdAction(id, includes);
                tsw.Stop();
                timings.Add("ArtistByIdAction", tsw.ElapsedMilliseconds);
                return rr;
            }, data.Artist.CacheRegionUrn(id));
            if (result?.Data != null && roadieUser != null)
            {
                tsw.Restart();
                var artist = await GetArtist(id);
                tsw.Stop();
                timings.Add("getArtist", tsw.ElapsedMilliseconds);
                tsw.Restart();
                var userBookmarkResult = await BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Artist);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x?.Bookmark?.Value == artist?.RoadieId.ToString()) != null;
                }
                tsw.Stop();
                timings.Add("userBookmarkResult", tsw.ElapsedMilliseconds);
                tsw.Restart();
                var userArtist = DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == roadieUser.Id);
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

                if (result.Data.Comments.Any())
                {
                    tsw.Restart();
                    var commentIds = result.Data.Comments.Select(x => x.DatabaseId).ToArray();
                    var userCommentReactions = (from cr in DbContext.CommentReactions
                                                where commentIds.Contains(cr.CommentId)
                                                where cr.UserId == roadieUser.Id
                                                select cr).ToArray();
                    foreach (var comment in result.Data.Comments)
                    {
                        var userCommentReaction = userCommentReactions.FirstOrDefault(x => x.CommentId == comment.DatabaseId);
                        comment.IsDisliked = userCommentReaction?.ReactionValue == CommentReaction.Dislike;
                        comment.IsLiked = userCommentReaction?.ReactionValue == CommentReaction.Like;
                    }

                    tsw.Stop();
                    timings.Add("userCommentReactions", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ById Artist: `{ result?.Data }`, includes [{ includes.ToCSV() }], timings [{ timings.ToTimings() }]");
            return new OperationResult<Artist>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> Delete(Library.Identity.User user, data.Artist artist, bool deleteFolder)
        {
            var isSuccess = false;
            try
            {
                if (artist != null)
                {
                    DbContext.Artists.Remove(artist);
                    await DbContext.SaveChangesAsync();
                    if(deleteFolder)
                    {
                        // Delete all image files for Artist                
                        foreach (var file in ImageHelper.ImageFilesInFolder(artist.ArtistFileFolder(Configuration), SearchOption.TopDirectoryOnly))
                        {
                            try
                            {
                                File.Delete(file);
                                Logger.LogWarning("For Artist [{0}], Deleted File [{1}]", artist.Id, file);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, $"Error Deleting File [{file}] Exception [{ex}]");
                            }
                        }

                        var artistDir = new DirectoryInfo(artist.ArtistFileFolder(Configuration));
                        FolderPathHelper.DeleteEmptyFolders(artistDir.Parent);
                    }
                    await BookmarkService.RemoveAllBookmarksForItem(BookmarkType.Artist, artist.Id);
                    await UpdatePlaylistCountsForArtist(artist.Id, DateTime.UtcNow);
                    CacheManager.ClearRegion(artist.CacheRegion);
                    Logger.LogWarning("User `{0}` deleted Artist `{1}]`", user, artist);
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
                return new OperationResult<bool>
                {
                    Errors = new Exception[1] { ex }
                };
            }

            return new OperationResult<bool>
            {
                IsSuccess = isSuccess,
                Data = isSuccess
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<ArtistList>> List(Library.Models.Users.User roadieUser, PagedRequest request, bool? doRandomize = false, bool? onlyIncludeWithReleases = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            int? rowCount = null;

            IQueryable<int> favoriteArtistIds = null;
            if (request.FilterFavoriteOnly)
            {
                favoriteArtistIds = from a in DbContext.Artists
                                    join ua in DbContext.UserArtists on a.Id equals ua.ArtistId
                                    where ua.IsFavorite ?? false
                                    where roadieUser == null || ua.UserId == roadieUser.Id
                                    select a.Id;
            }
            IQueryable<int> labelArtistIds = null;
            if (request.FilterToLabelId.HasValue)
            {
                labelArtistIds = (from l in DbContext.Labels
                                  join rl in DbContext.ReleaseLabels on l.Id equals rl.LabelId
                                  join r in DbContext.Releases on rl.ReleaseId equals r.Id
                                  where l.RoadieId == request.FilterToLabelId
                                  select r.ArtistId)
                                  .Distinct();
            }
            IQueryable<int> genreArtistIds = null;
            var isFilteredToGenre = false;
            if (request.FilterToGenreId.HasValue)
            {
                genreArtistIds = (from ag in DbContext.ArtistGenres
                                  join g in DbContext.Genres on ag.GenreId equals g.Id
                                  where g.RoadieId == request.FilterToGenreId
                                  select ag.ArtistId)
                  .Distinct();
                isFilteredToGenre = true;
            }
            else if (!string.IsNullOrEmpty(request.Filter) && request.Filter.StartsWith(":genre", StringComparison.OrdinalIgnoreCase))
            {
                var genreFilter = request.Filter.Replace(":genre ", "");
                genreArtistIds = (from ag in DbContext.ArtistGenres
                                  join g in DbContext.Genres on ag.GenreId equals g.Id
                                  where g.Name.Contains(genreFilter)
                                  select ag.ArtistId)
                                  .Distinct();
                isFilteredToGenre = true;
                request.Filter = null;
            }
            var onlyWithReleases = onlyIncludeWithReleases ?? true;
            var isEqualFilter = false;
            if (!string.IsNullOrEmpty(request.FilterValue))
            {
                var filter = request.FilterValue;
                // if filter string is wrapped in quotes then is an exact not like search, e.g. "Diana Ross" should not return "Diana Ross & The Supremes"
                if (filter.StartsWith('"') && filter.EndsWith('"'))
                {
                    isEqualFilter = true;
                    request.Filter = filter.Substring(1, filter.Length - 2);
                }
            }
            var normalizedFilterValue = !string.IsNullOrEmpty(request.FilterValue)
                ? request.FilterValue.ToAlphanumericName()
                : null;

            int[] randomArtistIds = null;
            SortedDictionary<int, int> randomArtistData = null;
            if (doRandomize ?? false)
            {
                var randomLimit = request.Limit ?? roadieUser?.RandomReleaseLimit ?? request.LimitValue;
                randomArtistData = await DbContext.RandomArtistIds(roadieUser?.Id ?? -1, randomLimit, request.FilterFavoriteOnly, request.FilterRatedOnly);
                randomArtistIds = randomArtistData.Select(x => x.Value).ToArray();
                rowCount = DbContext.Artists.Count();
            }
            var result = (from a in DbContext.Artists
                          where !onlyWithReleases || a.ReleaseCount > 0
                          where randomArtistIds == null || randomArtistIds.Contains(a.Id)
                          where request.FilterToArtistId == null || a.RoadieId == request.FilterToArtistId
                          where request.FilterMinimumRating == null || a.Rating >= request.FilterMinimumRating.Value
                          where request.FilterValue == "" ||
                                a.Name.Contains(request.FilterValue) ||
                                a.SortName.Contains(request.FilterValue) ||
                                a.RealName.Contains(request.FilterValue) ||
                                a.AlternateNames.Contains(request.FilterValue) ||
                                a.AlternateNames.Contains(normalizedFilterValue)
                          where !isEqualFilter ||
                                a.Name.Equals(request.FilterValue) ||
                                a.SortName.Equals(request.FilterValue) ||
                                a.RealName.Equals(request.FilterValue) ||
                                a.AlternateNames.Equals(request.FilterValue) ||
                                a.AlternateNames.Equals(normalizedFilterValue)
                          where !request.FilterFavoriteOnly || favoriteArtistIds.Contains(a.Id)
                          where request.FilterToLabelId == null || labelArtistIds.Contains(a.Id)
                          where !isFilteredToGenre || genreArtistIds.Contains(a.Id)
                          select new ArtistList
                          {
                              DatabaseId = a.Id,
                              Id = a.RoadieId,
                              Artist = new DataToken
                              {
                                  Text = a.Name,
                                  Value = a.RoadieId.ToString()
                              },
                              Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, a.RoadieId),
                              Rating = a.Rating,
                              Rank = (double?)a.Rank,
                              CreatedDate = a.CreatedDate,
                              LastUpdated = a.LastUpdated,
                              LastPlayed = a.LastPlayed,
                              PlayedCount = a.PlayedCount,
                              ReleaseCount = a.ReleaseCount,
                              TrackCount = a.TrackCount,
                              SortName = a.SortName,
                              Status = a.Status
                          });

            ArtistList[] rows;
            rowCount = rowCount ?? result.Count();

            if (doRandomize ?? false)
            {
                var resultData = await result.ToArrayAsync();
                rows = (from r in resultData
                        join ra in randomArtistData on r.DatabaseId equals ra.Value
                        orderby ra.Key
                        select r
                       ).ToArray();
            }
            else
            {
                string sortBy;
                if (request.ActionValue == Library.Models.Users.User.ActionKeyUserRated)
                {
                    sortBy = string.IsNullOrEmpty(request.Sort)
                        ? request.OrderValue(new Dictionary<string, string> { { "Rating", "DESC" }, { "Artist.Text", "ASC" } })
                        : request.OrderValue();
                }
                else
                {
                    sortBy = request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Artist.Text", "ASC" } });
                }
                rows = await result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArrayAsync();
            }

            if (rows.Any() && roadieUser != null)
            {
                var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                var userArtistRatings = await (from ua in DbContext.UserArtists
                                             where ua.UserId == roadieUser.Id
                                             where rowIds.Contains(ua.ArtistId)
                                             select ua).ToArrayAsync();

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
            if (!string.IsNullOrEmpty(request.Filter) && rowCount == 0)
            {
                if (Configuration.RecordNoResultSearches)
                {
                    // Create request for no artist found
                    var req = new data.Request
                    {
                        UserId = roadieUser?.Id,
                        Description = request.Filter
                    };
                    DbContext.Requests.Add(req);
                    await DbContext.SaveChangesAsync();
                }
            }
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<ArtistList>
            {
                TotalCount = rowCount.Value,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            };
        }

        /// <summary>
        ///     Merge one Artist into another one
        /// </summary>
        /// <param name="artistToMerge">The Artist to be merged</param>
        /// <param name="artistToMergeInto">The Artist to merge into</param>
        /// <returns></returns>
        public async Task<OperationResult<bool>> MergeArtists(Library.Identity.User user, Guid artistToMergeId, Guid artistToMergeIntoId)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var artistToMerge = DbContext.Artists
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .FirstOrDefault(x => x.RoadieId == artistToMergeId);
            if (artistToMerge == null)
            {
                Logger.LogWarning("MergeArtists Unknown Artist [{0}]", artistToMergeId);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistToMergeId}]");
            }

            var mergeIntoArtist = DbContext.Artists
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .FirstOrDefault(x => x.RoadieId == artistToMergeIntoId);
            if (mergeIntoArtist == null)
            {
                Logger.LogWarning("MergeArtists Unknown Artist [{0}]", artistToMergeIntoId);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistToMergeIntoId}]");
            }

            try
            {
                var result = await MergeArtists(user, artistToMerge, mergeIntoArtist);
                if (!result.IsSuccess)
                {
                    CacheManager.ClearRegion(artistToMerge.CacheRegion);
                    CacheManager.ClearRegion(mergeIntoArtist.CacheRegion);
                    Logger.LogWarning("MergeArtists `{0}` => `{1}`, By User `{2}`", artistToMerge, mergeIntoArtist,
                        user);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
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

        public async Task<OperationResult<bool>> RefreshArtistMetadata(Library.Identity.User user, Guid artistId)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(artistId != Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
                if (artist == null)
                {
                    Logger.LogWarning("Unable To Find Artist [{0}]", artistId);
                    return new OperationResult<bool>();
                }

                OperationResult<data.Artist> artistSearch = null;
                try
                {
                    artistSearch = await ArtistLookupEngine.PerformMetaDataProvidersArtistSearch(new AudioMetaData
                    {
                        Artist = artist.Name
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Serialize());
                }

                if (artistSearch.IsSuccess)
                {
                    // Do metadata search for Artist like if new Artist then set some overides and merge
                    var mergeResult = await MergeArtists(user, artistSearch.Data, artist);
                    if (mergeResult.IsSuccess)
                    {
                        artist = mergeResult.Data;
                        await DbContext.SaveChangesAsync();
                        sw.Stop();
                        CacheManager.ClearRegion(artist.CacheRegion);
                        Logger.LogTrace("Scanned RefreshArtistMetadata [{0}], OperationTime [{1}]",
                            artist.ToString(), sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        sw.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
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

        public async Task<OperationResult<bool>> ScanArtistReleasesFolders(Library.Identity.User user, Guid artistId, string destinationFolder, bool doJustInfo)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(artistId != Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var artist = DbContext.Artists
                    .Include("Releases")
                    .Include("Releases.Labels")
                    .FirstOrDefault(x => x.RoadieId == artistId);
                if (artist == null)
                {
                    Logger.LogWarning("Unable To Find Artist [{0}]", artistId);
                    return new OperationResult<bool>();
                }
                var releaseScannedCount = 0;
                var artistFolder = artist.ArtistFileFolder(Configuration);
                if (!Directory.Exists(artistFolder))
                {
                    Logger.LogDebug($"ScanArtistReleasesFolders: ArtistFolder Not Found [{ artistFolder }] For Artist `{ artist }`");
                    return new OperationResult<bool>();
                }
                var scannedArtistFolders = new List<string>();
                // Scan known releases for changes
                if (artist.Releases != null)
                {
                    foreach (var release in artist.Releases)
                        try
                        {
                            result = result && (await ReleaseService.ScanReleaseFolder(user, Guid.Empty, doJustInfo, release)).Data;
                            releaseScannedCount++;
                            scannedArtistFolders.Add(release.ReleaseFileFolder(artistFolder));
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, ex.Serialize());
                        }
                }
                var artistImage = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistFolder), ImageType.Artist).FirstOrDefault();
                if (artistImage != null)
                {
                    // See if image file is valid image if not delete it 
                    if (ImageHelper.ConvertToJpegFormat(File.ReadAllBytes(artistImage.FullName)) == null)
                    {
                        artistImage.Delete();
                    }
                }
                // Any folder found in Artist folder not already scanned scan
                var nonReleaseFolders = from d in Directory.EnumerateDirectories(artistFolder)
                                        where !(from r in scannedArtistFolders select r).Contains(d)
                                        orderby d
                                        select d;
                foreach (var folder in nonReleaseFolders)
                {
                    await FileDirectoryProcessorService.Process(user, new DirectoryInfo(folder), doJustInfo);
                }
                if (!doJustInfo)
                {
                    Services.FileDirectoryProcessorService.DeleteEmptyFolders(new DirectoryInfo(artistFolder), Logger);
                }
                sw.Stop();
                CacheManager.ClearRegion(artist.CacheRegion);
                Logger.LogInformation("Scanned Artist [{0}], Releases Scanned [{1}], OperationTime [{2}]", artist.ToString(), releaseScannedCount, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
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

        public async Task<OperationResult<Library.Models.Image>> SetReleaseImageByUrl(Library.Identity.User user, Guid id, string imageUrl)
        {
            return await SaveImageBytes(user, id, WebHelper.BytesForImageUrl(imageUrl));
        }

        public async Task<OperationResult<bool>> UpdateArtist(Library.Identity.User user, Artist model)
        {
            var didRenameArtist = false;
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = DbContext.Artists
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .FirstOrDefault(x => x.RoadieId == model.Id);
            if (artist == null)
            {
                return new OperationResult<bool>(true, $"Artist Not Found [{model.Id}]");
            }
            // If artist is being renamed, see if artist already exists with new model supplied name
            var artistName = artist.SortNameValue;
            var artistModelName = model.SortNameValue;
            if (artistName.ToAlphanumericName() != artistModelName.ToAlphanumericName())
            {
                var existingArtist = DbContext.Artists.FirstOrDefault(x => x.Name == model.Name || x.SortName == model.SortName);
                if (existingArtist != null)
                {
                    return new OperationResult<bool>($"Artist already exists `{ existingArtist }` with name [{artistModelName }].");
                }
            }
            try
            {
                var now = DateTime.UtcNow;
                var originalArtistFolder = artist.ArtistFileFolder(Configuration);
                var specialArtistName = model.Name.ToAlphanumericName();
                var alt = new List<string>(model.AlternateNamesList);
                if (!model.AlternateNamesList.Contains(specialArtistName, StringComparer.OrdinalIgnoreCase))
                {
                    alt.Add(specialArtistName);
                }
                artist.AlternateNames = alt.ToDelimitedList();
                artist.ArtistType = model.ArtistType;
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

                var newArtistFolder = artist.ArtistFileFolder(Configuration);
                // Rename artist folder to reflect new artist name
                if (!newArtistFolder.Equals(originalArtistFolder, StringComparison.OrdinalIgnoreCase))
                {
                    // If folder already exists for new artist name that means another artist has that folder (usually sort name)
                    if (Directory.Exists(newArtistFolder))
                    {
                        // Set sortname to be unique and try again
                        var oldSortName = artist.SortName;
                        artist.SortName = $"{ artist.SortName} [{ artist.Id }]";
                        Logger.LogTrace($"Updated Artist SortName From [{ oldSortName }] to [{ artist.SortName }]");
                        newArtistFolder = artist.ArtistFileFolder(Configuration);
                        if (Directory.Exists(newArtistFolder))
                        {
                            return new OperationResult<bool>($"Artist Folder [{ newArtistFolder }] already exists.");
                        }
                    }
                    didRenameArtist = true;
                    if (Directory.Exists(originalArtistFolder))
                    {
                        Logger.LogTrace($"Moving Artist From Folder [{originalArtistFolder}] ->  [{newArtistFolder}]");
                        Directory.Move(originalArtistFolder, newArtistFolder);
                    }
                }
                if (!Directory.Exists(newArtistFolder))
                {
                    Directory.CreateDirectory(newArtistFolder);
                }

                var artistImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (artistImage != null)
                {
                    // Save unaltered image to cover file
                    var artistImageName = Path.Combine(newArtistFolder, ImageHelper.ArtistImageFilename);
                    File.WriteAllBytes(artistImageName, ImageHelper.ConvertToJpegFormat(artistImage));
                }

                if (model.NewSecondaryImagesData != null && model.NewSecondaryImagesData.Any())
                {
                    // Additional images to add to artist
                    var looper = 0;
                    foreach (var newSecondaryImageData in model.NewSecondaryImagesData)
                    {
                        var artistSecondaryImage = ImageHelper.ImageDataFromUrl(newSecondaryImageData);
                        if (artistSecondaryImage != null)
                        {
                            // Ensure is jpeg first
                            artistSecondaryImage = ImageHelper.ConvertToJpegFormat(artistSecondaryImage);

                            var artistImageFilename = Path.Combine(newArtistFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                            while (File.Exists(artistImageFilename))
                            {
                                looper++;
                                artistImageFilename = Path.Combine(newArtistFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                            }

                            File.WriteAllBytes(artistImageFilename, artistSecondaryImage);
                        }

                        looper++;
                    }
                }

                if (model.Genres != null && model.Genres.Any())
                {
                    // Remove existing Genres not in model list
                    foreach (var genre in artist.Genres.ToList())
                    {
                        var doesExistInModel = model.Genres.Any(x => SafeParser.ToGuid(x.Genre.Value) == genre.Genre.RoadieId);
                        if (!doesExistInModel) artist.Genres.Remove(genre);
                    }

                    // Add new Genres in model not in data
                    foreach (var genre in model.Genres)
                    {
                        var genreId = SafeParser.ToGuid(genre.Genre.Value);
                        var doesExistInData = artist.Genres.Any(x => x.Genre.RoadieId == genreId);
                        if (!doesExistInData)
                        {
                            var g = DbContext.Genres.FirstOrDefault(x => x.RoadieId == genreId);
                            if (g != null)
                                artist.Genres.Add(new data.ArtistGenre
                                {
                                    ArtistId = artist.Id,
                                    GenreId = g.Id,
                                    Genre = g
                                });
                        }
                    }
                }
                else if (model.Genres == null || !model.Genres.Any())
                {
                    artist.Genres.Clear();
                }

                if (model.AssociatedArtistsTokens != null && model.AssociatedArtistsTokens.Any())
                {
                    var associatedArtists = DbContext.ArtistAssociations.Include(x => x.AssociatedArtist)
                        .Where(x => x.ArtistId == artist.Id).ToList();

                    // Remove existing AssociatedArtists not in model list
                    foreach (var associatedArtist in associatedArtists)
                    {
                        var doesExistInModel = model.AssociatedArtistsTokens.Any(x =>
                            SafeParser.ToGuid(x.Value) == associatedArtist.AssociatedArtist.RoadieId);
                        if (!doesExistInModel) DbContext.ArtistAssociations.Remove(associatedArtist);
                    }

                    // Add new AssociatedArtists in model not in data
                    foreach (var associatedArtist in model.AssociatedArtistsTokens)
                    {
                        var associatedArtistId = SafeParser.ToGuid(associatedArtist.Value);
                        var doesExistInData =
                            associatedArtists.Any(x => x.AssociatedArtist.RoadieId == associatedArtistId);
                        if (!doesExistInData)
                        {
                            var a = DbContext.Artists.FirstOrDefault(x => x.RoadieId == associatedArtistId);
                            if (a != null)
                                DbContext.ArtistAssociations.Add(new data.ArtistAssociation
                                {
                                    ArtistId = artist.Id,
                                    AssociatedArtistId = a.Id
                                });
                        }
                    }
                }
                else if (model.AssociatedArtistsTokens == null || !model.AssociatedArtistsTokens.Any())
                {
                    var associatedArtists = DbContext.ArtistAssociations.Include(x => x.AssociatedArtist).Where(x => x.ArtistId == artist.Id || x.AssociatedArtistId == artist.Id).ToList();
                    DbContext.ArtistAssociations.RemoveRange(associatedArtists);
                }

                if (model.SimilarArtistsTokens != null && model.SimilarArtistsTokens.Any())
                {
                    var similarArtists = DbContext.ArtistSimilar.Include(x => x.SimilarArtist)
                        .Where(x => x.ArtistId == artist.Id).ToList();

                    // Remove existing AssociatedArtists not in model list
                    foreach (var similarArtist in similarArtists)
                    {
                        var doesExistInModel = model.SimilarArtistsTokens.Any(x =>
                            SafeParser.ToGuid(x.Value) == similarArtist.SimilarArtist.RoadieId);
                        if (!doesExistInModel) DbContext.ArtistSimilar.Remove(similarArtist);
                    }

                    // Add new SimilarArtists in model not in data
                    foreach (var similarArtist in model.SimilarArtistsTokens)
                    {
                        var similarArtistId = SafeParser.ToGuid(similarArtist.Value);
                        var doesExistInData = similarArtists.Any(x => x.SimilarArtist.RoadieId == similarArtistId);
                        if (!doesExistInData)
                        {
                            var a = DbContext.Artists.FirstOrDefault(x => x.RoadieId == similarArtistId);
                            if (a != null)
                                DbContext.ArtistSimilar.Add(new data.ArtistSimilar
                                {
                                    ArtistId = artist.Id,
                                    SimilarArtistId = a.Id
                                });
                        }
                    }
                }
                else if (model.SimilarArtistsTokens == null || !model.SimilarArtistsTokens.Any())
                {
                    var similarArtists = DbContext.ArtistSimilar.Include(x => x.SimilarArtist).Where(x => x.ArtistId == artist.Id || x.SimilarArtistId == artist.Id).ToList();
                    DbContext.ArtistSimilar.RemoveRange(similarArtists);
                }
                artist.LastUpdated = now;
                await DbContext.SaveChangesAsync();
                if (didRenameArtist)
                {
                    // Many contributing artists do not have releases and will not have an empty Artist folder
                    if (Directory.Exists(newArtistFolder))
                    {
                        // Update artist tracks to have new artist name in ID3 metadata
                        foreach (var mp3 in Directory.GetFiles(newArtistFolder, "*.mp3", SearchOption.AllDirectories))
                        {
                            var trackFileInfo = new FileInfo(mp3);
                            var audioMetaData = await AudioMetaDataHelper.GetInfo(trackFileInfo);
                            if (audioMetaData != null)
                            {
                                audioMetaData.Artist = artist.Name;
                                AudioMetaDataHelper.WriteTags(audioMetaData, trackFileInfo);
                            }
                        }

                        await ScanArtistReleasesFolders(user, artist.RoadieId, Configuration.LibraryFolder, false);
                    }
                }

                CacheManager.ClearRegion(artist.CacheRegion);
                Logger.LogInformation($"UpdateArtist `{artist}` By User `{user}`: Renamed Artist [{didRenameArtist}]");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
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

        public async Task<OperationResult<Library.Models.Image>> UploadArtistImage(Library.Identity.User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }

            return await SaveImageBytes(user, id, bytes);
        }

        private async Task<OperationResult<Artist>> ArtistByIdAction(Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var artist = await GetArtist(id);
            tsw.Stop();
            timings.Add("getArtist", tsw.ElapsedMilliseconds);

            if (artist == null) return new OperationResult<Artist>(true, $"Artist Not Found [{id}]");
            tsw.Restart();
            var result = artist.Adapt<Artist>();
            result.BandStatus = result.BandStatus ?? BandStatus.Unknown.ToString();
            result.BeginDate = result.BeginDate == null || result.BeginDate == DateTime.MinValue
                ? null
                : result.BeginDate;
            result.EndDate = result.EndDate == null || result.EndDate == DateTime.MinValue ? null : result.EndDate;
            result.BirthDate = result.BirthDate == null || result.BirthDate == DateTime.MinValue
                ? null
                : result.BirthDate;
            result.RankPosition = result.Rank > 0
                ? SafeParser.ToNumber<int?>(DbContext.Artists.Count(x => (double?)x.Rank > result.Rank) + 1)
                : null;
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);
            tsw.Restart();
            result.Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, id);
            result.MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "artist", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);


            if (includes != null && includes.Any())
            {
                tsw.Restart();
                if(includes.Contains("genres"))
                {
                    var artistGenreIds = artist.Genres.Select(x => x.GenreId).ToArray();
                    result.Genres = (await (from g in DbContext.Genres
                                            let releaseCount = (from rg in DbContext.ReleaseGenres
                                                                where rg.GenreId == g.Id
                                                                select rg.Id).Count()
                                            let artistCount = (from rg in DbContext.ArtistGenres
                                                               where rg.GenreId == g.Id
                                                               select rg.Id).Count()
                                            where artistGenreIds.Contains(g.Id)
                                            select new { g, releaseCount, artistCount }).ToListAsync())
                                         .Select(x => GenreList.FromDataGenre(x.g,
                                                                             ImageHelper.MakeGenreThumbnailImage(Configuration, HttpContext, x.g.RoadieId),
                                                                             x.artistCount,
                                                                             x.releaseCount))
                                         .ToArray();

                    tsw.Stop();
                    timings.Add("genres", tsw.ElapsedMilliseconds);

                }


                if (includes.Contains("releases"))
                {
                    tsw.Restart();
                    var dtoReleases = new List<ReleaseList>();
                    foreach (var release in await DbContext.Releases
                                            .Include("Medias")
                                            .Include("Medias.Tracks")
                                            .Include("Medias.Tracks")
                                            .Where(x => x.ArtistId == artist.Id)
                                            .ToArrayAsync())
                    {
                        var releaseList = release.Adapt<ReleaseList>();
                        releaseList.Thumbnail = ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, release.RoadieId);
                        var dtoReleaseMedia = new List<ReleaseMediaList>();
                        if (includes.Contains("tracks"))
                            foreach (var releasemedia in release.Medias.OrderBy(x => x.MediaNumber).ToArray())
                            {
                                var dtoMedia = releasemedia.Adapt<ReleaseMediaList>();
                                var tracks = new List<TrackList>();
                                foreach (var t in await DbContext.Tracks
                                                            .Where(x => x.ReleaseMediaId == releasemedia.Id)
                                                            .OrderBy(x => x.TrackNumber)
                                                            .ToArrayAsync())
                                {
                                    var track = t.Adapt<TrackList>();
                                    ArtistList trackArtist = null;
                                    if (t.ArtistId.HasValue)
                                    {
                                        var ta = DbContext.Artists.FirstOrDefault(x => x.Id == t.ArtistId.Value);
                                        if (ta != null)
                                            trackArtist = ArtistList.FromDataArtist(ta, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, ta.RoadieId));
                                    }
                                    track.TrackArtist = trackArtist;
                                    tracks.Add(track);
                                }
                                dtoMedia.Tracks = tracks;
                                dtoReleaseMedia.Add(dtoMedia);
                            }
                        releaseList.Media = dtoReleaseMedia;
                        dtoReleases.Add(releaseList);
                    }

                    result.Releases = dtoReleases;
                    tsw.Stop();
                    timings.Add("releases", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("stats"))
                    try
                    {
                        tsw.Restart();
                        var artistTracks = from r in DbContext.Releases
                                           join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                           join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                           where r.ArtistId == artist.Id || t.ArtistId == artist.Id
                                           select new
                                           {
                                               t.Id,
                                               size = t.FileSize,
                                               time = t.Duration,
                                               isMissing = t.Hash == null
                                           };
                        var validCartistTracks = artistTracks.Where(x => !x.isMissing);
                        var trackTime = validCartistTracks.Sum(x => (long?)x.time);
                        result.Statistics = new CollectionStatistics
                        {
                            FileSize = artistTracks.Sum(x => (long?)x.size).ToFileSize(),
                            MissingTrackCount = artistTracks.Count(x => x.isMissing),
                            ReleaseCount = artist.ReleaseCount,
                            ReleaseMediaCount = (from r in DbContext.Releases
                                                 join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                                 where r.ArtistId == artist.Id
                                                 select rm.Id).Count(),
                            TrackTime = validCartistTracks.Any()
                                ? new TimeInfo((decimal)trackTime).ToFullFormattedString()
                                : "--:--",
                            TrackCount = validCartistTracks.Count(),
                            TrackPlayedCount = artist.PlayedCount
                        };
                        tsw.Stop();
                        timings.Add("stats", tsw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Error Getting Statistics for Artist `{artist}`");
                    }

                if (includes.Contains("images"))
                {
                    tsw.Restart();
                    var artistFolder = artist.ArtistFileFolder(Configuration);
                    var artistImagesInFolder = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistFolder), ImageType.ArtistSecondary, SearchOption.TopDirectoryOnly);
                    if (artistImagesInFolder.Any())
                    {
                        result.Images = artistImagesInFolder.Select((x, i) => ImageHelper.MakeFullsizeSecondaryImage(Configuration, HttpContext, id, ImageType.ArtistSecondary, i));
                    }

                    tsw.Stop();
                    timings.Add("images", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("associatedartists"))
                {
                    tsw.Restart();
                    var associatedWithArtists = await (from aa in DbContext.ArtistAssociations
                                                 join a in DbContext.Artists on aa.AssociatedArtistId equals a.Id
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
                                                     Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, a.RoadieId),
                                                     Rating = a.Rating,
                                                     Rank = (double?)a.Rank,
                                                     CreatedDate = a.CreatedDate,
                                                     LastUpdated = a.LastUpdated,
                                                     LastPlayed = a.LastPlayed,
                                                     PlayedCount = a.PlayedCount,
                                                     ReleaseCount = a.ReleaseCount,
                                                     TrackCount = a.TrackCount,
                                                     SortName = a.SortName
                                                 }).ToArrayAsync();

                    var associatedArtists = await (from aa in DbContext.ArtistAssociations
                                             join a in DbContext.Artists on aa.ArtistId equals a.Id
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
                                                 Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, a.RoadieId),
                                                 Rating = a.Rating,
                                                 Rank = (double?)a.Rank,
                                                 CreatedDate = a.CreatedDate,
                                                 LastUpdated = a.LastUpdated,
                                                 LastPlayed = a.LastPlayed,
                                                 PlayedCount = a.PlayedCount,
                                                 ReleaseCount = a.ReleaseCount,
                                                 TrackCount = a.TrackCount,
                                                 SortName = a.SortName
                                             }).ToArrayAsync();
                    result.AssociatedArtists = associatedArtists.Union(associatedWithArtists, new ArtistListComparer()).OrderBy(x => x.SortName);
                    result.AssociatedArtistsTokens = result.AssociatedArtists.Select(x => x.Artist).ToArray();
                    tsw.Stop();
                    timings.Add("associatedartists", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("similarartists"))
                {
                    tsw.Restart();
                    var similarWithArtists = await (from aa in DbContext.ArtistSimilar
                                              join a in DbContext.Artists on aa.SimilarArtistId equals a.Id
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
                                                  Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, a.RoadieId),
                                                  Rating = a.Rating,
                                                  Rank = (double?)a.Rank,
                                                  CreatedDate = a.CreatedDate,
                                                  LastUpdated = a.LastUpdated,
                                                  LastPlayed = a.LastPlayed,
                                                  PlayedCount = a.PlayedCount,
                                                  ReleaseCount = a.ReleaseCount,
                                                  TrackCount = a.TrackCount,
                                                  SortName = a.SortName
                                              }).ToArrayAsync();

                    var similarArtists = await (from aa in DbContext.ArtistSimilar
                                          join a in DbContext.Artists on aa.ArtistId equals a.Id
                                          where aa.SimilarArtistId == artist.Id
                                          select new ArtistList
                                          {
                                              DatabaseId = a.Id,
                                              Id = a.RoadieId,
                                              Artist = new DataToken
                                              {
                                                  Text = a.Name,
                                                  Value = a.RoadieId.ToString()
                                              },
                                              Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, a.RoadieId),
                                              Rating = a.Rating,
                                              Rank = (double?)a.Rank,
                                              CreatedDate = a.CreatedDate,
                                              LastUpdated = a.LastUpdated,
                                              LastPlayed = a.LastPlayed,
                                              PlayedCount = a.PlayedCount,
                                              ReleaseCount = a.ReleaseCount,
                                              TrackCount = a.TrackCount,
                                              SortName = a.SortName
                                          }).ToArrayAsync();
                    result.SimilarArtists = similarWithArtists.Union(similarArtists, new ArtistListComparer()).OrderBy(x => x.SortName);
                    result.SimilarArtistsTokens = result.SimilarArtists.Select(x => x.Artist).ToArray();
                    tsw.Stop();
                    timings.Add("similarartists", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("collections"))
                {
                    tsw.Restart();
                    var collectionPagedRequest = new PagedRequest
                    {
                        Limit = 100
                    };
                    var r = await CollectionService.List(null, collectionPagedRequest, artistId: artist.RoadieId);
                    if (r.IsSuccess) result.CollectionsWithArtistReleases = r.Rows.ToArray();
                    tsw.Stop();
                    timings.Add("collections", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("comments"))
                {
                    tsw.Restart();
                    var artistComments = await DbContext.Comments.Include(x => x.User)
                                        .Where(x => x.ArtistId == artist.Id)
                                        .OrderByDescending(x => x.CreatedDate)
                                        .ToArrayAsync();
                    if (artistComments.Any())
                    {
                        var comments = new List<Comment>();
                        var commentIds = artistComments.Select(x => x.Id).ToArray();
                        var userCommentReactions = (from cr in DbContext.CommentReactions
                                                    where commentIds.Contains(cr.CommentId)
                                                    select cr).ToArray();
                        foreach (var artistComment in artistComments)
                        {
                            var comment = artistComment.Adapt<Comment>();
                            comment.DatabaseId = artistComment.Id;
                            comment.User = UserList.FromDataUser(artistComment.User,
                                ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, artistComment.User.RoadieId));
                            comment.DislikedCount = userCommentReactions.Count(x =>
                                x.CommentId == artistComment.Id && x.ReactionValue == CommentReaction.Dislike);
                            comment.LikedCount = userCommentReactions.Count(x =>
                                x.CommentId == artistComment.Id && x.ReactionValue == CommentReaction.Like);
                            comments.Add(comment);
                        }

                        result.Comments = comments;
                    }

                    tsw.Stop();
                    timings.Add("comments", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("playlists"))
                {
                    tsw.Restart();
                    var pg = new PagedRequest
                    {
                        FilterToArtistId = artist.RoadieId
                    };
                    var r = await PlaylistService.List(pg);
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
                    var artistContributingTracks = (from t in DbContext.Tracks
                                                    join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                    join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                                    where t.ArtistId == artist.Id
                                                    select r.Id)
                                                    .Distinct();

                    if (artistContributingTracks?.Any() ?? false)
                    {
                        result.ArtistContributionReleases = (await (from r in DbContext.Releases.Include(x => x.Artist)
                                                             where artistContributingTracks.Contains(r.Id)
                                                             select r)
                                                             .OrderBy(x => x.Title)
                                                             .ToArrayAsync())
                                                             .Select(r => ReleaseList.FromDataRelease(r, r.Artist, HttpContext.BaseUrl, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, r.Artist.RoadieId), ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, r.RoadieId)));

                        result.ArtistContributionReleases = result.ArtistContributionReleases.Any()
                            ? result.ArtistContributionReleases
                            : null;
                    }
                    tsw.Stop();
                    timings.Add("contributions", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("labels"))
                {
                    tsw.Restart();
                    result.ArtistLabels = (await (from l in DbContext.Labels
                                           join rl in DbContext.ReleaseLabels on l.Id equals rl.LabelId
                                           join r in DbContext.Releases on rl.ReleaseId equals r.Id
                                           where r.ArtistId == artist.Id
                                           orderby l.SortName
                                           select LabelList.FromDataLabel(l, ImageHelper.MakeLabelThumbnailImage(Configuration, HttpContext, l.RoadieId)))
                                          .ToArrayAsync())
                                          .GroupBy(x => x.Label.Value)
                                          .Select(x => x.First())
                                          .OrderBy(x => x.SortName)
                                          .ThenBy(x => x.Label.Text)
                                          .ToArray();
                    result.ArtistLabels = result.ArtistLabels.Any() ? result.ArtistLabels : null;
                    tsw.Stop();
                    timings.Add("labels", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ByIdAction: Artist `{ artist }`: includes [{includes.ToCSV() }], timings: [{ timings.ToTimings() }]");
            return new OperationResult<Artist>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private OperationResult<data.Artist> GetByExternalIds(string musicBrainzId = null, string iTunesId = null, string amgId = null, string spotifyId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var artist = (from a in DbContext.Artists
                          where a.MusicBrainzId != null && musicBrainzId != null && a.MusicBrainzId == musicBrainzId ||
                                a.ITunesId != null || iTunesId != null && a.ITunesId == iTunesId || a.AmgId != null ||
                                amgId != null && a.AmgId == amgId || a.SpotifyId != null ||
                                spotifyId != null && a.SpotifyId == spotifyId
                          select a).FirstOrDefault();
            sw.Stop();
            if (artist == null || !artist.IsValid)
                Logger.LogTrace(
                    "ArtistFactory: Artist Not Found By External Ids: MusicbrainzId [{0}], iTunesIs [{1}], AmgId [{2}], SpotifyId [{3}]",
                    musicBrainzId, iTunesId, amgId, spotifyId);
            return new OperationResult<data.Artist>
            {
                IsSuccess = artist != null,
                OperationTime = sw.ElapsedMilliseconds,
                Data = artist
            };
        }

        private async Task<OperationResult<data.Artist>> MergeArtists(Library.Identity.User user, data.Artist artistToMerge, data.Artist artistToMergeInto)
        {
            SimpleContract.Requires<ArgumentNullException>(artistToMerge != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentNullException>(artistToMergeInto != null, "Invalid Artist");

            var result = false;
            var now = DateTime.UtcNow;

            var sw = new Stopwatch();
            sw.Start();

            var artistToMergeFolder = artistToMerge.ArtistFileFolder(Configuration);
            var artistToMergeIntoFolder = artistToMergeInto.ArtistFileFolder(Configuration);

            artistToMergeInto.RealName = artistToMergeInto.RealName ?? artistToMerge.RealName;
            artistToMergeInto.MusicBrainzId = artistToMergeInto.MusicBrainzId ?? artistToMerge.MusicBrainzId;
            artistToMergeInto.ITunesId = artistToMergeInto.ITunesId ?? artistToMerge.ITunesId;
            artistToMergeInto.AmgId = artistToMergeInto.AmgId ?? artistToMerge.AmgId;
            artistToMergeInto.SpotifyId = artistToMergeInto.SpotifyId ?? artistToMerge.SpotifyId;
            artistToMergeInto.Profile = artistToMergeInto.Profile ?? artistToMerge.Profile;
            artistToMergeInto.BirthDate = artistToMergeInto.BirthDate ?? artistToMerge.BirthDate;
            artistToMergeInto.BeginDate = artistToMergeInto.BeginDate ?? artistToMerge.BeginDate;
            artistToMergeInto.EndDate = artistToMergeInto.EndDate ?? artistToMerge.EndDate;
            if (!string.IsNullOrEmpty(artistToMerge.ArtistType) && !artistToMerge.ArtistType.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                artistToMergeInto.ArtistType = artistToMergeInto.ArtistType ?? artistToMerge.ArtistType;
            }
            artistToMergeInto.BioContext = artistToMergeInto.BioContext ?? artistToMerge.BioContext;
            artistToMergeInto.DiscogsId = artistToMergeInto.DiscogsId ?? artistToMerge.DiscogsId;
            artistToMergeInto.Tags = artistToMergeInto.Tags.AddToDelimitedList(artistToMerge.Tags.ToListFromDelimited());
            var altNames = artistToMerge.AlternateNames.ToListFromDelimited().ToList();
            altNames.Add(artistToMerge.Name);
            altNames.Add(artistToMerge.SortName);
            artistToMergeInto.AlternateNames = artistToMergeInto.AlternateNames.AddToDelimitedList(altNames);
            artistToMergeInto.URLs = artistToMergeInto.URLs.AddToDelimitedList(artistToMerge.URLs.ToListFromDelimited());
            artistToMergeInto.ISNI = artistToMergeInto.ISNI.AddToDelimitedList(artistToMerge.ISNI.ToListFromDelimited());
            artistToMergeInto.LastUpdated = now;

            try
            {
                var artistGenres = DbContext.ArtistGenres.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                if (artistGenres != null)
                {
                    var existingArtistGenres = DbContext.ArtistGenres.Where(x => x.ArtistId == artistToMergeInto.Id).ToArray();
                    foreach (var artistGenre in artistGenres)
                    {
                        var existing = existingArtistGenres.FirstOrDefault(x => x.GenreId == artistGenre.GenreId);
                        // If not exist then add new for artist to merge into
                        if (existing == null)
                        {
                            DbContext.ArtistGenres.Add(new data.ArtistGenre
                            {
                                ArtistId = artistToMergeInto.Id,
                                GenreId = artistGenre.GenreId
                            });
                        }
                    }
                }
                try
                {
                    // Move any Artist and Artist Secondary images from ArtistToMerge into ArtistToMergeInto folder
                    if (Directory.Exists(artistToMergeFolder))
                    {
                        var artistToMergeImages = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistToMergeFolder), ImageType.Artist);
                        var artistToMergeSecondaryImages = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistToMergeFolder), ImageType.ArtistSecondary).ToList();
                        // Primary Artist image
                        if (artistToMergeImages.Any())
                        {
                            // If the ArtistToMergeInto already has a primary image then the ArtistToMerge primary image becomes a secondary image
                            var artistToMergeIntoPrimaryImage = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistToMergeIntoFolder), ImageType.Artist).FirstOrDefault();
                            if (artistToMergeIntoPrimaryImage != null)
                            {
                                artistToMergeSecondaryImages.Add(artistToMergeImages.First());
                            }
                            else
                            {
                                var artistImageFilename = Path.Combine(artistToMergeIntoFolder, ImageHelper.ArtistImageFilename);
                                artistToMergeImages.First().MoveTo(artistImageFilename);
                            }
                        }
                        // Secondary Artist images
                        if (artistToMergeSecondaryImages.Any())
                        {
                            var looper = 0;
                            foreach (var artistSecondaryImage in artistToMergeSecondaryImages)
                            {
                                var artistImageFilename = Path.Combine(artistToMergeIntoFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                                while (File.Exists(artistImageFilename))
                                {
                                    looper++;
                                    artistImageFilename = Path.Combine(artistToMergeIntoFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                                }
                                artistSecondaryImage.MoveTo(artistImageFilename);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "MergeArtists: Error Moving Artist Primary and Secondary Images");
                }

                var userArtists = DbContext.UserArtists.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                var artistTracks = DbContext.Tracks.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                if (artistTracks != null)
                {
                    foreach (var artistTrack in artistTracks)
                    {
                        artistTrack.ArtistId = artistToMergeInto.Id;
                    }
                }
                var artistReleases = DbContext.Releases.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                if (artistReleases != null)
                {
                    foreach (var artistRelease in artistReleases)
                    {
                        // See if there is already a release by the same name for the artist to merge into, if so then merge releases
                        var artistToMergeHasRelease = DbContext.Releases.FirstOrDefault(x => x.ArtistId == artistToMerge.Id && x.Title == artistRelease.Title);
                        if (artistToMergeHasRelease != null)
                        {
                            await ReleaseService.MergeReleases(user, artistRelease, artistToMergeHasRelease, false);
                        }
                        else
                        {
                            artistRelease.ArtistId = artistToMerge.Id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex.ToString());
            }

            foreach (var release in DbContext.Releases.Include("Artist").Where(x => x.ArtistId == artistToMerge.Id).ToArray())
            {
                var originalReleaseFolder = release.ReleaseFileFolder(artistToMergeFolder);
                await ReleaseService.UpdateRelease(user, release.Adapt<Release>(), originalReleaseFolder);
            }
            await DbContext.SaveChangesAsync();

            await Delete(user, artistToMerge, true);

            result = true;

            sw.Stop();
            return new OperationResult<data.Artist>
            {
                Data = artistToMergeInto,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Library.Models.Image>> SaveImageBytes(Library.Identity.User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == id);
            if (artist == null) return new OperationResult<Library.Models.Image>(true, $"Artist Not Found [{id}]");
            try
            {
                var now = DateTime.UtcNow;
                if (imageBytes != null)
                {
                    // Ensure artist folder exists
                    var artistFolder = artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Directory.CreateDirectory(artistFolder);
                        Logger.LogTrace("Created Artist Folder [0] for `artist`", artistFolder, artist);
                    }

                    // Save unaltered image to artist file
                    var artistImage = Path.Combine(artistFolder, ImageHelper.ArtistImageFilename);
                    File.WriteAllBytes(artistImage, ImageHelper.ConvertToJpegFormat(imageBytes));
                }
                artist.LastUpdated = now;
                await DbContext.SaveChangesAsync();
                CacheManager.ClearRegion(artist.CacheRegion);
                Logger.LogInformation($"SaveImageBytes `{artist}` By User `{user}`");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<Library.Models.Image>
            {
                IsSuccess = !errors.Any(),
                Data = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "artist", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }
    }
}