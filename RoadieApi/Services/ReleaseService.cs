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
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class ReleaseService : ServiceBase, IReleaseService
    {
        private IBookmarkService BookmarkService { get; } = null;
        private ICollectionService CollectionService { get; } = null;

        private IPlaylistService PlaylistService { get; } = null;

        public ReleaseService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ICollectionService collectionService,
                             IPlaylistService playlistService,
                             ILogger<ReleaseService> logger,
                             IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            this.CollectionService = collectionService;
            this.PlaylistService = playlistService;
            this.BookmarkService = bookmarkService;
        }

        public async Task<OperationResult<Release>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:release_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Release>>(cacheKey, async () =>
            {
                return await this.ReleaseByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            if (result?.Data != null && roadieUser != null)
            {
                var release = this.GetRelease(id);
                var userBookmarkResult = await this.BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Release);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Value == release.RoadieId.ToString()) != null;
                }
                if (result.Data.Medias != null)
                {
                    var releaseTrackIds = result.Data.Medias.SelectMany(x => x.Tracks).Select(x => x.Id);
                    var releaseUserTracks = (from ut in this.DbContext.UserTracks
                                             join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                             where ut.UserId == roadieUser.Id
                                             where (from x in releaseTrackIds select x).Contains(t.RoadieId)
                                             select new
                                             {
                                                 t,
                                                 ut
                                             }).ToArray();
                    if (releaseUserTracks != null && releaseUserTracks.Any())
                    {
                        foreach (var releaseUserTrack in releaseUserTracks)
                        {
                            foreach (var media in result.Data.Medias)
                            {
                                var releaseTrack = media.Tracks.FirstOrDefault(x => x.Id == releaseUserTrack.t.RoadieId);
                                if (releaseTrack != null)
                                {
                                    releaseTrack.UserRating = new UserTrack
                                    {
                                        Rating = releaseUserTrack.ut.Rating,
                                        IsDisliked = releaseUserTrack.ut.IsDisliked ?? false,
                                        IsFavorite = releaseUserTrack.ut.IsFavorite ?? false,
                                        LastPlayed = releaseUserTrack.ut.LastPlayed,
                                        PlayedCount = releaseUserTrack.ut.PlayedCount
                                    };
                                }
                            }
                        }
                    }
                }
                var userRelease = this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == roadieUser.Id);
                if (userRelease != null)
                {
                    result.Data.UserRating = new UserRelease
                    {
                        IsDisliked = userRelease.IsDisliked ?? false,
                        IsFavorite = userRelease.IsFavorite ?? false,
                        Rating = userRelease.Rating
                    };
                }
            }
            sw.Stop();
            return new OperationResult<Release>(result.Messages)
            {
                Data = result?.Data,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<ReleaseList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            IEnumerable<int> collectionReleaseIds = null;
            if (request.FilterToCollectionId.HasValue)
            {
                collectionReleaseIds = (from cr in this.DbContext.CollectionReleases
                                        join c in this.DbContext.Collections on cr.CollectionId equals c.Id
                                        join r in this.DbContext.Releases on cr.ReleaseId equals r.Id
                                        where c.RoadieId == request.FilterToCollectionId.Value
                                        orderby cr.ListNumber
                                        select r.Id).Skip(request.SkipValue).Take(request.LimitValue).ToArray();

            }
            int[] favoriteReleaseIds = new int[0];
            if (request.FilterFavoriteOnly)
            {
                favoriteReleaseIds = (from a in this.DbContext.Releases
                                      join ur in this.DbContext.UserReleases on a.Id equals ur.ReleaseId
                                      where ur.IsFavorite ?? false
                                      where (roadieUser == null || ur.UserId == roadieUser.Id)
                                      select a.Id
                                     ).ToArray();
            }
            int[] genreReleaseIds = new int[0];
            if (!string.IsNullOrEmpty(request.FilterByGenre))
            {
                genreReleaseIds = (from rg in this.DbContext.ReleaseGenres
                                   join g in this.DbContext.Genres on rg.GenreId equals g.Id
                                   where g.Name == request.FilterByGenre
                                   select rg.ReleaseId).ToArray();
            }
            if (request.FilterFromYear.HasValue || request.FilterToYear.HasValue)
            {
                // If from is larger than to then reverse values and set sort order to desc
                if (request.FilterToYear > request.FilterFromYear)
                {
                    var t = request.FilterToYear;
                    request.FilterToYear = request.FilterFromYear;
                    request.FilterFromYear = t;
                    request.Order = "DESC";
                }
                else
                {
                    request.Order = "ASC";
                }
            }

            //
            // TODO list should honor disliked artist and albums for random
            //
            var result = (from r in this.DbContext.Releases
                          join a in this.DbContext.Artists on r.ArtistId equals a.Id
                          where (request.FilterMinimumRating == null || r.Rating >= request.FilterMinimumRating.Value)
                          where (request.FilterToArtistId == null || r.Artist.RoadieId == request.FilterToArtistId)
                          where (request.FilterToCollectionId == null || collectionReleaseIds.Contains(r.Id))
                          where (!request.FilterFavoriteOnly || favoriteReleaseIds.Contains(r.Id))
                          where (request.FilterByGenre == null || genreReleaseIds.Contains(r.Id))
                          where (request.FilterFromYear == null || r.ReleaseDate != null && r.ReleaseDate.Value.Year <= request.FilterFromYear)
                          where (request.FilterToYear == null || r.ReleaseDate != null && r.ReleaseDate.Value.Year >= request.FilterToYear)
                          where (request.FilterValue == "" || (r.Title.Contains(request.FilterValue) || r.AlternateNames.Contains(request.FilterValue)))
                          select new ReleaseList
                          {
                              DatabaseId = r.Id,
                              Id = r.RoadieId,
                              Artist = new DataToken
                              {
                                  Value = a.RoadieId.ToString(),
                                  Text = a.Name
                              },
                              Release = new DataToken
                              {
                                  Text = r.Title,
                                  Value = r.RoadieId.ToString()
                              },
                              ArtistThumbnail = this.MakeArtistThumbnailImage(a.RoadieId),
                              CreatedDate = r.CreatedDate,
                              Duration = r.Duration,
                              LastPlayed = r.LastPlayed,
                              LastUpdated = r.LastUpdated,
                              LibraryStatus = r.LibraryStatus,
                              MediaCount = r.MediaCount,
                              Rating = r.Rating,
                              ReleaseDateDateTime = r.ReleaseDate,
                              ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ r.RoadieId}",
                              Status = r.Status,
                              Thumbnail = this.MakeReleaseThumbnailImage(r.RoadieId),
                              TrackCount = r.TrackCount,
                              TrackPlayedCount = r.PlayedCount
                          }
                          ).Distinct();
            ReleaseList[] rows = null;

            var rowCount = result.Count();

            if (doRandomize ?? false)
            {
                var randomLimit = roadieUser?.RandomReleaseLimit ?? 100;
                request.Limit = request.LimitValue > randomLimit ? randomLimit : request.LimitValue;
                rows = result.OrderBy(x => Guid.NewGuid()).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            else
            {
                string sortBy = null;
                if (request.ActionValue == User.ActionKeyUserRated)
                {
                    sortBy = request.OrderValue(new Dictionary<string, string> { { "Rating", "DESC" } });
                }
                else if (request.FilterToArtistId.HasValue)
                {
                    sortBy = request.OrderValue(new Dictionary<string, string> { { "ReleaseDate", "ASC" }, { "Release.Text", "ASC" } });
                }
                else
                {
                    sortBy = request.OrderValue(new Dictionary<string, string> { { "Release.Text", "ASC" } });
                }
                if (request.FilterRatedOnly)
                {
                    result = result.Where(x => x.Rating.HasValue);
                }
                if (request.FilterMinimumRating.HasValue)
                {
                    result = result.Where(x => x.Rating.HasValue && x.Rating.Value >= request.FilterMinimumRating.Value);
                }
                if (request.FilterToCollectionId.HasValue)
                {
                    rows = result.ToArray();
                }
                else
                {
                    rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
                }
            }
            if (rows.Any())
            {
                var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                var genreData = (from rg in this.DbContext.ReleaseGenres
                                 join g in this.DbContext.Genres on rg.GenreId equals g.Id
                                 where rowIds.Contains(rg.ReleaseId)
                                 orderby rg.Id
                                 select new
                                 {
                                     rg.ReleaseId,
                                     dt = new DataToken
                                     {
                                         Text = g.Name,
                                         Value = g.RoadieId.ToString()
                                     }
                                 }).ToArray();

                foreach (var release in rows)
                {
                    var genre = genreData.FirstOrDefault(x => x.ReleaseId == release.DatabaseId);
                    release.Genre = genre?.dt ?? new DataToken();
                }

                if (request.FilterToCollectionId.HasValue)
                {
                    var newRows = new List<ReleaseList>(rows);
                    var collection = this.GetCollection(request.FilterToCollectionId.Value);
                    var collectionReleases = (from c in this.DbContext.Collections
                                              join cr in this.DbContext.CollectionReleases on c.Id equals cr.CollectionId
                                              where c.RoadieId == request.FilterToCollectionId
                                              where collectionReleaseIds.Contains(cr.ReleaseId)
                                              orderby cr.ListNumber
                                              select cr);
                    foreach (var par in collection.PositionArtistReleases().OrderBy(x => x.Index).Skip(request.SkipValue).Take(request.LimitValue))
                    {
                        var cr = collectionReleases.FirstOrDefault(x => x.ListNumber == par.Position);
                        // Release is known for Collection CSV, find newRow and update ListNumber
                        if (cr != null)
                        {
                            var parRelease = rows.FirstOrDefault(x => x.DatabaseId == cr.ReleaseId);
                            if(parRelease != null)
                            {
                                if(!parRelease.ListNumber.HasValue)
                                {
                                    parRelease.ListNumber = par.Position;
                                }
                                else
                                {
                                    var anotherInstanceOfReleaseInCollection = parRelease.ShallowCopy();
                                    anotherInstanceOfReleaseInCollection.ListNumber = par.Position;
                                    newRows.Add(anotherInstanceOfReleaseInCollection);
                                }
                            }
                        }
                        // Release is not known add missing dummy release to rows
                        else
                        {
                            newRows.Add(new ReleaseList
                            {
                                Artist = new DataToken
                                {
                                    Text = par.Artist
                                },
                                Release = new DataToken
                                {
                                    Text = par.Release
                                },
                                CssClass = "missing",
                                ArtistThumbnail = new Image($"{this.HttpContext.ImageBaseUrl }/unknown.jpg"),
                                Thumbnail = new Image($"{this.HttpContext.ImageBaseUrl }/unknown.jpg"),
                                ListNumber = par.Position
                            });
                        }
                    }
                    // Resort the list for the collection by listNumber
                    rows = newRows.OrderBy(x => x.ListNumber).ToArray();
                    rowCount = collection.CollectionCount;
                }

                if (roadieUser != null)
                {
                    var userReleaseRatings = (from ur in this.DbContext.UserReleases
                                              where ur.UserId == roadieUser.Id
                                              where rowIds.Contains(ur.ReleaseId)
                                              select ur).ToArray();

                    foreach (var userReleaseRating in userReleaseRatings.Where(x => rows.Select(r => r.DatabaseId).Contains(x.ReleaseId)))
                    {
                        var row = rows.FirstOrDefault(x => x.DatabaseId == userReleaseRating.ReleaseId);
                        if (row != null)
                        {
                            var isDisliked = userReleaseRating.IsDisliked ?? false;
                            var isFavorite = userReleaseRating.IsFavorite ?? false;
                            row.UserRating = new UserRelease
                            {
                                IsDisliked = isDisliked,
                                IsFavorite = isFavorite,
                                Rating = userReleaseRating.Rating,
                                RatedDate = isDisliked || isFavorite ? (DateTime?)(userReleaseRating.LastUpdated ?? userReleaseRating.CreatedDate) : null
                            };
                        }
                    }
                }
            }
            if (includes != null && includes.Any())
            {
                if (includes.Contains("tracks"))
                {
                    var releaseIds = rows.Select(x => x.Id).ToArray();
                    var artistTracks = (from r in this.DbContext.Releases
                                        join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                        join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                        join a in this.DbContext.Artists on r.ArtistId equals a.Id
                                        where (releaseIds.Contains(r.RoadieId))
                                        orderby r.Id, rm.MediaNumber, t.TrackNumber
                                        select new
                                        {
                                            t,
                                            releaseMedia = rm,
                                            release = r
                                        });
                    var releaseTrackIds = artistTracks.Select(x => x.t.Id).ToList();
                    var artistUserTracks = (from ut in this.DbContext.UserTracks
                                            where ut.UserId == roadieUser.Id
                                            where (from x in releaseTrackIds select x).Contains(ut.TrackId)
                                            select ut).ToArray();
                    foreach (var release in rows)
                    {
                        var releaseMedias = new List<ReleaseMediaList>();
                        foreach (var releaseMedia in artistTracks.Where(x => x.release.RoadieId == release.Id).Select(x => x.releaseMedia).Distinct().ToArray())
                        {
                            var rm = releaseMedia.Adapt<ReleaseMediaList>();
                            var rmTracks = new List<TrackList>();
                            foreach (var track in artistTracks.Where(x => x.t.ReleaseMediaId == releaseMedia.Id).OrderBy(x => x.t.TrackNumber).ToArray())
                            {
                                var t = track.t.Adapt<TrackList>();
                                t.CssClass = string.IsNullOrEmpty(track.t.Hash) ? "Missing" : "Ok";
                                t.TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ track.t.RoadieId}.mp3";
                                var userRating = artistUserTracks.FirstOrDefault(x => x.TrackId == track.t.Id);
                                if (userRating != null)
                                {
                                    t.UserRating = new UserTrack
                                    {
                                        Rating = userRating.Rating,
                                        IsFavorite = userRating.IsFavorite ?? false,
                                        IsDisliked = userRating.IsDisliked ?? false
                                    };
                                }
                                rmTracks.Add(t);
                            }
                            rm.Tracks = rmTracks;
                            releaseMedias.Add(rm);
                        }
                        release.Media = releaseMedias.OrderBy(x => x.MediaNumber).ToArray();
                    }
                }
            }
            if (request.FilterFavoriteOnly)
            {
                rows = rows.OrderBy(x => x.UserRating.Rating).ToArray();
            }
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<ReleaseList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            };
        }

        public async Task<FileOperationResult<byte[]>> ReleaseZipped(User roadieUser, Guid id)
        {
            var release = this.GetRelease(id);
            if (release == null)
            {
                return new FileOperationResult<byte[]>(true, string.Format("Release Not Found [{0}]", id));
            }

            byte[] zipBytes = null;
            string zipFileName = null;
            try
            {
                var artistFolder = release.Artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                var releaseFolder = release.ReleaseFileFolder(artistFolder);
                if (!Directory.Exists(releaseFolder))
                {
                    this.Logger.LogCritical($"Release Folder [{ releaseFolder }] not found for Release `{ release }`");
                    return new FileOperationResult<byte[]>(true, string.Format("Release Folder Not Found [{0}]", id));
                }
                var releaseFiles = Directory.GetFiles(releaseFolder);
                using (MemoryStream zipStream = new MemoryStream())
                {
                    using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        foreach (var releaseFile in releaseFiles)
                        {
                            var fileInfo = new FileInfo(releaseFile);
                            if (fileInfo.Extension.ToLower() == ".mp3" || fileInfo.Extension.ToLower() == ".jpg")
                            {
                                ZipArchiveEntry entry = zip.CreateEntry(fileInfo.Name);
                                using (Stream entryStream = entry.Open())
                                {
                                    using (FileStream s = fileInfo.OpenRead())
                                    {
                                        s.CopyTo(entryStream);
                                    }
                                }
                            }
                        }
                    }
                    zipBytes = zipStream.ToArray();
                }
                zipFileName = $"{ release.Artist.Name }_{release.Title}.zip".ToFileNameFriendly();
                this.Logger.LogInformation($"User `{ roadieUser }` downloaded Release `{ release }` ZipFileName [{ zipFileName }], Zip Size [{ zipBytes?.Length }]");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error creating zip for Release `{0}`", release.ToString());
            }
            return new FileOperationResult<byte[]>
            {
                IsSuccess = zipBytes != null,
                Data = zipBytes,
                AdditionalData = new Dictionary<string, object> { { "ZipFileName", zipFileName } }
            };
        }

        private async Task<OperationResult<Release>> ReleaseByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var release = this.GetRelease(id);

            if (release == null)
            {
                return new OperationResult<Release>(true, string.Format("Release Not Found [{0}]", id));
            }
            var result = release.Adapt<Release>();
            result.Artist = ArtistList.FromDataArtist(release.Artist, this.MakeArtistThumbnailImage(release.Artist.RoadieId));
            result.Thumbnail = this.MakeReleaseThumbnailImage(release.RoadieId);
            result.MediumThumbnail = base.MakeThumbnailImage(id, "release", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            result.ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ release.RoadieId}";
            result.Profile = release.Profile;
            result.ReleaseDate = release.ReleaseDate.Value;
            result.MediaCount = release.MediaCount;
            result.TrackCount = release.TrackCount;
            result.CreatedDate = release.CreatedDate;
            result.LastUpdated = release.LastUpdated;
            result.AlternateNames = release.AlternateNames;
            result.Tags = release.Tags;
            result.URLs = release.URLs;

            if (release.SubmissionId.HasValue)
            {
                var submission = this.DbContext.Submissions.Include(x => x.User).FirstOrDefault(x => x.Id == release.SubmissionId);
                if (submission != null)
                {
                    if (!submission.User.IsPrivate ?? false)
                    {
                        result.Submission = new ReleaseSubmission
                        {
                            User = new DataToken
                            {
                                Text = submission.User.UserName,
                                Value = submission.User.RoadieId.ToString()
                            },
                            UserThumbnail = this.MakeUserThumbnailImage(submission.User.RoadieId),
                            SubmittedDate = submission.CreatedDate
                        };
                    }
                }
            }
            if (includes != null && includes.Any())
            {
                if (includes.Contains("genres"))
                {
                    result.Genres = release.Genres.Select(x => new DataToken
                    {
                        Text = x.Genre.Name,
                        Value = x.Genre.RoadieId.ToString()
                    });
                }
                if (includes.Contains("stats"))
                {
                    // TODO move these to release properties to speed up fetch times
                    var releaseTracks = (from r in this.DbContext.Releases
                                         join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                         join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                         where r.Id == release.Id
                                         select new
                                         {
                                             id = t.Id,
                                             size = t.FileSize,
                                             time = t.Duration,
                                             isMissing = t.Hash == null
                                         });
                    var releaseMedias = (from r in this.DbContext.Releases
                                         join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                         where r.Id == release.Id
                                         select new
                                         {
                                             rm.Id,
                                             rm.MediaNumber
                                         });
                    var releaseTime = releaseTracks.Sum(x => x.time);
                    var releaseStats = new ReleaseStatistics
                    {
                        MediaCount = release.MediaCount,
                        MissingTrackCount = releaseTracks.Where(x => x.isMissing).Count(),
                        TrackCount = release.TrackCount,
                        TrackPlayedCount = release.PlayedCount,
                        TrackSize = releaseTracks.Sum(x => (long?)x.size).ToFileSize(),
                        TrackTime = releaseTracks.Any() ? new TimeInfo((decimal)releaseTime).ToFullFormattedString() : "--:--"
                    };
                    result.MaxMediaNumber = releaseMedias.Max(x => x.MediaNumber);
                    result.Statistics = releaseStats;
                    result.MediaCount = release.MediaCount ?? (short?)releaseStats.MediaCount;
                }
                if (includes.Contains("images"))
                {
                    var releaseImages = this.DbContext.Images.Where(x => x.ReleaseId == release.Id).Select(x => MakeFullsizeImage(x.RoadieId, x.Caption)).ToArray();
                    if (releaseImages != null && releaseImages.Any())
                    {
                        result.Images = releaseImages;
                    }
                }
                if (includes.Contains("playlists"))
                {
                    var pg = new PagedRequest
                    {
                        FilterToReleaseId = release.RoadieId
                    };
                    var r = await this.PlaylistService.List(pg);
                    if (r.IsSuccess)
                    {
                        result.Playlists = r.Rows.ToArray();
                    }
                }
                if (includes.Contains("labels"))
                {
                    var releaseLabels = (from l in this.DbContext.Labels
                                         join rl in this.DbContext.ReleaseLabels on l.Id equals rl.LabelId
                                         where rl.ReleaseId == release.Id
                                         orderby rl.BeginDate, l.Name
                                         select new
                                         {
                                             l,
                                             rl
                                         }).ToArray();
                    if (releaseLabels != null)
                    {
                        var labels = new List<ReleaseLabel>();
                        foreach (var releaseLabel in releaseLabels)
                        {
                            var rl = new ReleaseLabel
                            {
                                BeginDate = releaseLabel.rl.BeginDate,
                                EndDate = releaseLabel.rl.EndDate,
                                CatalogNumber = releaseLabel.rl.CatalogNumber,
                                Label = new LabelList
                                {
                                    Id = releaseLabel.rl.RoadieId,
                                    Label = new DataToken
                                    {
                                        Text = releaseLabel.l.Name,
                                        Value = releaseLabel.l.RoadieId.ToString()
                                    },
                                    SortName = releaseLabel.l.SortName,
                                    CreatedDate = releaseLabel.l.CreatedDate,
                                    LastUpdated = releaseLabel.l.LastUpdated,
                                    ArtistCount = releaseLabel.l.ArtistCount,
                                    ReleaseCount = releaseLabel.l.ReleaseCount,
                                    TrackCount = releaseLabel.l.TrackCount,
                                    Thumbnail = MakeLabelThumbnailImage(releaseLabel.l.RoadieId)
                                }
                            };
                            labels.Add(rl);
                        }
                        result.Labels = labels;
                    }
                }
                if (includes.Contains("collections"))
                {
                    var releaseCollections = this.DbContext.CollectionReleases.Include(x => x.Collection).Where(x => x.ReleaseId == release.Id).OrderBy(x => x.ListNumber).ToArray();
                    if (releaseCollections != null)
                    {
                        var collections = new List<ReleaseInCollection>();
                        foreach (var releaseCollection in releaseCollections)
                        {
                            collections.Add(new ReleaseInCollection
                            {
                                Collection = new CollectionList
                                {
                                    DatabaseId = releaseCollection.Collection.Id,
                                    Collection = new DataToken
                                    {
                                        Text = releaseCollection.Collection.Name,
                                        Value = releaseCollection.Collection.RoadieId.ToString()
                                    },
                                    Id = releaseCollection.Collection.RoadieId,
                                    CollectionCount = releaseCollection.Collection.CollectionCount,
                                    CollectionType = (releaseCollection.Collection.CollectionType ?? CollectionType.Unknown).ToString(),
                                    CollectionFoundCount = (from crc in this.DbContext.CollectionReleases
                                                            where crc.CollectionId == releaseCollection.Collection.Id
                                                            select crc.Id).Count(),
                                    CreatedDate = releaseCollection.Collection.CreatedDate,
                                    LastUpdated = releaseCollection.Collection.LastUpdated,
                                    Thumbnail = MakeCollectionThumbnailImage(releaseCollection.Collection.RoadieId)
                                },
                                ListNumber = releaseCollection.ListNumber
                            });
                        }
                        result.Collections = collections;
                    }
                }
                if (includes.Contains("tracks"))
                {
                    var releaseMedias = new List<ReleaseMediaList>();
                    foreach (var releaseMedia in release.Medias.OrderBy(x => x.MediaNumber))
                    {
                        var rm = releaseMedia.Adapt<ReleaseMediaList>();
                        var rmTracks = new List<TrackList>();
                        foreach (var track in releaseMedia.Tracks.OrderBy(x => x.TrackNumber))
                        {
                            var t = track.Adapt<TrackList>();
                            t.Track = new DataToken
                            {
                                Text = track.Title,
                                Value = track.RoadieId.ToString()
                            };
                            t.CssClass = string.IsNullOrEmpty(track.Hash) ? "Missing" : "Ok";
                            t.TrackArtist = track.TrackArtist != null ? ArtistList.FromDataArtist(track.TrackArtist, this.MakeArtistThumbnailImage(track.TrackArtist.RoadieId)) : null;
                            t.TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ t.Id}.mp3";
                            rmTracks.Add(t);
                        }
                        rm.Tracks = rmTracks;
                        releaseMedias.Add(rm);
                    }
                    result.Medias = releaseMedias;
                }
            }
            sw.Stop();
            return new OperationResult<Release>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}