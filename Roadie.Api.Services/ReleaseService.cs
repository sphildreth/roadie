﻿using Mapster;
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
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
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
using System.Net.Http;
using System.Threading.Tasks;
using data = Roadie.Library.Data;
using Image = Roadie.Library.Models.Image;
using Release = Roadie.Library.Models.Releases.Release;

namespace Roadie.Api.Services
{
    public class ReleaseService : HttpFactoryServiceBase<ReleaseService>, IReleaseService
    {
        private IArtistLookupEngine ArtistLookupEngine { get; }

        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        private IBookmarkService BookmarkService { get; }

        private IPlaylistService PlaylistService { get; }

        private readonly List<int> _addedTrackIds = new List<int>();

        public IEnumerable<int> AddedTrackIds => _addedTrackIds;

        public ReleaseService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            IPlaylistService playlistService,
            ILogger<ReleaseService> logger,
            IBookmarkService bookmarkService,
            IArtistLookupEngine artistLookupEngine,
            IAudioMetaDataHelper audioMetaDataHelper,
            IHttpClientFactory httpClientFactory
        )
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext, httpClientFactory)
        {
            PlaylistService = playlistService;
            BookmarkService = bookmarkService;
            ArtistLookupEngine = artistLookupEngine;
            AudioMetaDataHelper = audioMetaDataHelper;
        }

        private async Task<OperationResult<Release>> ReleaseByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var release = await GetRelease(id).ConfigureAwait(false);
            tsw.Stop();
            timings.Add("getRelease", tsw.ElapsedMilliseconds);

            if (release == null)
            {
                return new OperationResult<Release>(true, $"Release Not Found [{id}]");
            }
            tsw.Restart();
            var result = release.Adapt<Release>();
            result.Artist = ArtistList.FromDataArtist(release.Artist, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, release.Artist.RoadieId));
            result.Thumbnail = ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, release.RoadieId);
            result.MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "release", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            result.ReleasePlayUrl = $"{HttpContext.BaseUrl}/play/release/{release.RoadieId}";
            result.Profile = release.Profile;
            result.ReleaseDate = release.ReleaseDate.Value;
            result.MediaCount = release.MediaCount;
            result.TrackCount = release.TrackCount;
            result.CreatedDate = release.CreatedDate;
            result.LastUpdated = release.LastUpdated;
            result.AlternateNames = release.AlternateNames;
            result.Tags = release.Tags;
            result.URLs = release.URLs;
            result.RankPosition = result.Rank > 0
                ? SafeParser.ToNumber<int?>(DbContext.Releases.Count(x => (double?)x.Rank > result.Rank) + 1)
                : null;
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);
            if (release.SubmissionId.HasValue)
            {
                tsw.Restart();
                var submission = DbContext.Submissions.Include(x => x.User).FirstOrDefault(x => x.Id == release.SubmissionId);
                if (submission != null && (!submission.User.IsPrivate ?? false))
                {
                    result.Submission = new ReleaseSubmission
                    {
                        User = new DataToken
                        {
                            Text = submission.User.UserName,
                            Value = submission.User.RoadieId.ToString()
                        },
                        UserThumbnail = ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, submission.User.RoadieId),
                        SubmittedDate = submission.CreatedDate
                    };
                }
                tsw.Stop();
                timings.Add("submissions", tsw.ElapsedMilliseconds);
            }

            if (includes?.Any() == true)
            {
                if (includes.Contains("credits"))
                {
                    tsw.Restart();

                    result.Credits = (await (from c in DbContext.Credits
                                             join cc in DbContext.CreditCategory on c.CreditCategoryId equals cc.Id
                                             join a in DbContext.Artists on c.ArtistId equals a.Id into agg
                                             from a in agg.DefaultIfEmpty()
                                             where c.ReleaseId == release.Id
                                             select new { c, cc, a })
                                             .ToListAsync().ConfigureAwait(false))
                                             .Select(x => new CreditList
                                             {
                                                 Id = x.c.RoadieId,
                                                 Artist = x.a == null ? null : ArtistList.FromDataArtist(x.a, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, x.a.RoadieId)),
                                                 Category = new DataToken
                                                 {
                                                     Text = x.cc.Name,
                                                     Value = x.cc.RoadieId.ToString()
                                                 },
                                                 CreditName = x.a?.Name ?? x.c.CreditToName,
                                                 Description = x.c.Description
                                             }).ToArray();
                    tsw.Stop();
                    timings.Add("credits", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("genres"))
                {
                    tsw.Restart();
                    var releaseGenreIds = release.Genres.Select(x => x.GenreId).ToArray();
                    result.Genres = (await (from g in DbContext.Genres
                                            let releaseCount = (from rg in DbContext.ReleaseGenres
                                                                where rg.GenreId == g.Id
                                                                select rg.Id).Count()
                                            let artistCount = (from rg in DbContext.ArtistGenres
                                                               where rg.GenreId == g.Id
                                                               select rg.Id).Count()
                                            where releaseGenreIds.Contains(g.Id)
                                            select new { g, releaseCount, artistCount }).ToListAsync().ConfigureAwait(false))
                                         .Select(x => GenreList.FromDataGenre(x.g,
                                                                             ImageHelper.MakeGenreThumbnailImage(Configuration, HttpContext, x.g.RoadieId),
                                                                             x.artistCount,
                                                                             x.releaseCount))
                                         .ToArray();

                    tsw.Stop();
                    timings.Add("genres", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("stats"))
                {
                    tsw.Restart();
                    var releaseTracks = from r in DbContext.Releases
                                        join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                        join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                        where r.Id == release.Id
                                        select new
                                        {
                                            id = t.Id,
                                            size = t.FileSize,
                                            time = t.Duration,
                                            isMissing = t.Hash == null
                                        };
                    var releaseMedias = from r in DbContext.Releases
                                        join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                        where r.Id == release.Id
                                        select new
                                        {
                                            rm.Id,
                                            rm.MediaNumber
                                        };
                    var releaseTime = releaseTracks?.Sum(x => (long?)x.time) ?? 0;
                    var releaseStats = new ReleaseStatistics
                    {
                        MediaCount = release.MediaCount,
                        MissingTrackCount = releaseTracks?.Where(x => x.isMissing).Count(),
                        TrackCount = release.TrackCount,
                        TrackPlayedCount = release.PlayedCount,
                        TrackSize = releaseTracks?.Sum(x => (long?)x.size).ToFileSize(),
                        TrackTime = releaseTracks.Any() ? new TimeInfo(releaseTime).ToFullFormattedString() : "--:--"
                    };
                    result.MaxMediaNumber = releaseMedias.Any() ? releaseMedias.Max(x => x.MediaNumber) : (short)0;
                    result.Statistics = releaseStats;
                    result.MediaCount = release.MediaCount ?? (short?)releaseStats?.MediaCount;
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("images"))
                {
                    tsw.Restart();
                    var artistFolder = release.Artist.ArtistFileFolder(Configuration);
                    var releaseFolder = release.ReleaseFileFolder(artistFolder);
                    var releaseImagesInFolder = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(releaseFolder), ImageType.ReleaseSecondary, SearchOption.TopDirectoryOnly);
                    if (releaseImagesInFolder.Any())
                    {
                        result.Images = releaseImagesInFolder.Select((_, i) => ImageHelper.MakeFullsizeSecondaryImage(Configuration, HttpContext, id, ImageType.ReleaseSecondary, i));
                    }
                    tsw.Stop();
                    timings.Add("images", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("playlists"))
                {
                    tsw.Restart();
                    var pg = new PagedRequest
                    {
                        FilterToReleaseId = release.RoadieId
                    };
                    var r = await PlaylistService.ListAsync(pg).ConfigureAwait(false);
                    if (r.IsSuccess)
                    {
                        result.Playlists = r.Rows.ToArray();
                    }

                    tsw.Stop();
                    timings.Add("playlists", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("labels"))
                {
                    tsw.Restart();
                    var releaseLabels = (from l in DbContext.Labels
                                         join rl in DbContext.ReleaseLabels on l.Id equals rl.LabelId
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
                                CreatedDate = releaseLabel.rl.CreatedDate,
                                Id = releaseLabel.rl.RoadieId,
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
                                    Thumbnail = ImageHelper.MakeLabelThumbnailImage(Configuration, HttpContext, releaseLabel.l.RoadieId)
                                }
                            };
                            labels.Add(rl);
                        }

                        result.Labels = labels;
                    }
                    tsw.Stop();
                    timings.Add("labels", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("collections"))
                {
                    tsw.Restart();
                    var releaseCollections = DbContext.CollectionReleases.Include(x => x.Collection)
                                                      .Where(x => x.ReleaseId == release.Id)
                                                      .OrderBy(x => x.ListNumber)
                                                      .ToArray();
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
                                    CollectionType =
                                        (releaseCollection.Collection.CollectionType ?? CollectionType.Unknown)
                                        .ToString(),
                                    CollectionFoundCount = (from crc in DbContext.CollectionReleases
                                                            where crc.CollectionId == releaseCollection.Collection.Id
                                                            select crc.Id).Count(),
                                    CreatedDate = releaseCollection.Collection.CreatedDate,
                                    IsLocked = releaseCollection.Collection.IsLocked,
                                    LastUpdated = releaseCollection.Collection.LastUpdated,
                                    Thumbnail = ImageHelper.MakeCollectionThumbnailImage(Configuration, HttpContext, releaseCollection.Collection.RoadieId)
                                },
                                ListNumber = releaseCollection.ListNumber
                            });
                        }
                        result.Collections = collections;
                    }
                    tsw.Stop();
                    timings.Add("collections", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("comments"))
                {
                    tsw.Restart();
                    var releaseComments = DbContext.Comments.Include(x => x.User)
                                                            .Where(x => x.ReleaseId == release.Id)
                                                            .OrderByDescending(x => x.CreatedDate)
                                                            .ToArray();
                    if (releaseComments.Length > 0)
                    {
                        var comments = new List<Comment>();
                        var commentIds = releaseComments.Select(x => x.Id).ToArray();
                        var userCommentReactions = (from cr in DbContext.CommentReactions
                                                    where commentIds.Contains(cr.CommentId)
                                                    select cr).ToArray();
                        foreach (var releaseComment in releaseComments)
                        {
                            var comment = releaseComment.Adapt<Comment>();
                            comment.DatabaseId = releaseComment.Id;
                            comment.User = UserList.FromDataUser(releaseComment.User, ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, releaseComment.User.RoadieId));
                            comment.DislikedCount = userCommentReactions.Count(x => x.CommentId == releaseComment.Id && x.ReactionValue == CommentReaction.Dislike);
                            comment.LikedCount = userCommentReactions.Count(x => x.CommentId == releaseComment.Id && x.ReactionValue == CommentReaction.Like);
                            comments.Add(comment);
                        }
                        result.Comments = comments;
                    }
                    tsw.Stop();
                    timings.Add("comments", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("tracks"))
                {
                    tsw.Restart();
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
                            t.MediaNumber = rm.MediaNumber;
                            t.Status = track.Status;
                            t.TrackArtist = track.TrackArtist != null
                                ? ArtistList.FromDataArtist(track.TrackArtist,
                                    ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, track.TrackArtist.RoadieId))
                                : null;
                            rmTracks.Add(t);
                        }

                        rm.Tracks = rmTracks;
                        releaseMedias.Add(rm);
                    }
                    result.Medias = releaseMedias;
                    tsw.Stop();
                    timings.Add("tracks", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ByIdAction: Release `{ release }`: includes [{includes.ToCSV()}], timings: [{ timings.ToTimings() }]");
            return new OperationResult<Release>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Image>> SaveImageBytes(Library.Identity.User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var release = DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.RoadieId == id);
            if (release == null)
            {
                return new OperationResult<Image>(true, $"Release Not Found [{id}]");
            }
            try
            {
                var now = DateTime.UtcNow;
                imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);
                if (imageBytes != null)
                {
                    // Save unaltered image to cover file
                    var coverFileName = Path.Combine(release.ReleaseFileFolder(release.Artist.ArtistFileFolder(Configuration), true), "cover.jpg");
                    File.WriteAllBytes(coverFileName, imageBytes);

                    release.LastUpdated = now;
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    CacheManager.ClearRegion(release.CacheRegion);
                    Logger.LogInformation($"SaveImageBytes `{release}` By User `{user}`");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<Image>
            {
                IsSuccess = errors.Count == 0,
                Data = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "release", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<Release>> ByIdAsync(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = $"urn:release_by_id_operation:{id}:{(includes == null ? "0" : string.Join("|", includes))}";
            var result = await CacheManager.GetAsync(cacheKey, async () =>
             {
                 tsw.Restart();
                 var rr = await ReleaseByIdAction(id, includes).ConfigureAwait(false);
                 tsw.Stop();
                 timings.Add(nameof(ReleaseByIdAction), tsw.ElapsedMilliseconds);
                 return rr;
             }, data.Artist.CacheRegionUrn(id)).ConfigureAwait(false);
            if (result?.Data != null && roadieUser != null)
            {
                tsw.Restart();
                var release = await GetRelease(id).ConfigureAwait(false);
                tsw.Stop();
                timings.Add("getRelease", tsw.ElapsedMilliseconds);

                var userBookmarkResult = await BookmarkService.ListAsync(roadieUser, new PagedRequest(), false, BookmarkType.Release).ConfigureAwait(false);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x?.Bookmark?.Value == release?.RoadieId.ToString()) != null;
                }
                if (result.Data.Medias != null)
                {
                    tsw.Restart();
                    var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
                    tsw.Stop();
                    timings.Add("getUser", tsw.ElapsedMilliseconds);

                    tsw.Restart();
                    Parallel.ForEach(result.Data.Medias.SelectMany(x => x.Tracks), track => track.TrackPlayUrl = MakeTrackPlayUrl(user, HttpContext.BaseUrl, track.Id));

                    tsw.Stop();
                    timings.Add("makeTrackPlayUrls", tsw.ElapsedMilliseconds);

                    tsw.Restart();
                    var releaseTrackIds = result.Data.Medias.SelectMany(x => x.Tracks).Select(x => x.Id).ToList();
                    var releaseUserTracks = (from ut in DbContext.UserTracks
                                             join t in DbContext.Tracks on ut.TrackId equals t.Id
                                             where releaseTrackIds.Contains(t.RoadieId)
                                             where ut.UserId == roadieUser.Id
                                             select new
                                             {
                                                 t,
                                                 ut
                                             }).ToArray();
                    if (releaseUserTracks?.Any() == true)
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
                    tsw.Stop();
                    timings.Add("userTracks", tsw.ElapsedMilliseconds);
                }

                var userRelease = DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == roadieUser.Id);
                if (userRelease != null)
                {
                    tsw.Restart();
                    result.Data.UserRating = new UserRelease
                    {
                        IsDisliked = userRelease.IsDisliked ?? false,
                        IsFavorite = userRelease.IsFavorite ?? false,
                        Rating = userRelease.Rating
                    };
                    tsw.Stop();
                    timings.Add("userRelease", tsw.ElapsedMilliseconds);
                }
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
                        var userCommentReaction = Array.Find(userCommentReactions, x => x.CommentId == comment.DatabaseId);
                        comment.IsDisliked = userCommentReaction?.ReactionValue == CommentReaction.Dislike;
                        comment.IsLiked = userCommentReaction?.ReactionValue == CommentReaction.Like;
                    }
                    tsw.Stop();
                    timings.Add("userComments", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ById Release: `{ result?.Data }`, includes [{ includes.ToCSV() }], timings [{ timings.ToTimings() }]");
            return new OperationResult<Release>(result.Messages)
            {
                Data = result?.Data,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        /// <summary>
        ///     See if the given release has properties that have been modified that affect the folder structure, if so then handle
        ///     necessary operations for changes
        /// </summary>
        /// <param name="release">Release that has been modified</param>
        /// <param name="oldReleaseFolder">Folder for release before any changes</param>
        public async Task<OperationResult<bool>> CheckAndChangeReleaseTitle(data.Release release, string oldReleaseFolder)
        {
            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(oldReleaseFolder), "Invalid Release Old Folder");

            var sw = new Stopwatch();
            sw.Start();
            var now = DateTime.UtcNow;

            var result = false;
            var artistFolder = release.Artist.ArtistFileFolder(Configuration);
            var newReleaseFolder = release.ReleaseFileFolder(artistFolder);
            if (newReleaseFolder != oldReleaseFolder)
            {
                if (Directory.Exists(oldReleaseFolder))
                {
                    Directory.Move(oldReleaseFolder, newReleaseFolder);
                    Logger.LogInformation($"Moved Release Folder From [{ oldReleaseFolder }] -> [{ newReleaseFolder }]");
                }
                else if (!Directory.Exists(newReleaseFolder))
                {
                    Directory.CreateDirectory(newReleaseFolder);
                }
                var releaseDirectoryInfo = new DirectoryInfo(newReleaseFolder);
                // Update and move tracks under new release folder
                foreach (var releaseMedia in DbContext.ReleaseMedias.Where(x => x.ReleaseId == release.Id).ToArray())
                {
                    // Update the track path to have the new album title. This is needed because future scans might not work properly without updating track title.
                    foreach (var track in DbContext.Tracks.Where(x => x.ReleaseMediaId == releaseMedia.Id).ToArray())
                    {
                        track.FilePath = FolderPathHelper.TrackPath(Configuration, release.Artist, release, track);
                        var trackPath = track.PathToTrack(Configuration);
                        var trackFileInfo = new FileInfo(trackPath);
                        if (trackFileInfo.Exists)
                        {
                            // Update the tracks release tags
                            var audioMetaData = await AudioMetaDataHelper.GetInfo(trackFileInfo).ConfigureAwait(false);
                            audioMetaData.Release = release.Title;
                            AudioMetaDataHelper.WriteTags(audioMetaData, trackFileInfo);

                            // Update track path
                            track.Status = Statuses.Ok;
                            track.Hash = HashHelper.CreateMD5(release.ArtistId + trackFileInfo.LastWriteTimeUtc.GetHashCode().ToString() + audioMetaData.GetHashCode());
                            track.LastUpdated = now;
                        }
                        else
                        {
                            track.Hash = null;
                            track.Status = Statuses.Missing;
                        }
                        CacheManager.ClearRegion(track.CacheRegion);
                        result = true;
                    }
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                // Clean up any empty folders for the artist
                FolderPathHelper.DeleteEmptyFoldersForArtist(Configuration, release.Artist);
            }

            sw.Stop();
            CacheManager.ClearRegion(release.CacheRegion);
            if (release.Artist != null)
            {
                CacheManager.ClearRegion(release.Artist.CacheRegion);
            }

            return new OperationResult<bool>
            {
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteAsync(Library.Identity.User user, data.Release release, bool doDeleteFiles = false, bool doUpdateArtistCounts = true)
        {
            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(release.Artist != null, "Invalid Artist");

            var releaseCacheRegion = release.CacheRegion;
            var artistCacheRegion = release.Artist.CacheRegion;

            var result = false;
            var sw = new Stopwatch();
            sw.Start();
            var releaseTracks = await (from r in DbContext.Releases
                                       join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                       join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                       where r.Id == release.Id
                                       select t).ToArrayAsync().ConfigureAwait(false);

            var releaseLabelIds = await DbContext.ReleaseLabels.Where(x => x.ReleaseId == release.Id).Select(x => x.LabelId).ToArrayAsync().ConfigureAwait(false);

            var playlistIdsWithReleaseTracks = await (from r in DbContext.Releases
                                                      join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                                      join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                                      join plt in DbContext.PlaylistTracks on tr.Id equals plt.TrackId
                                                      where r.Id == release.Id
                                                      select plt.PlayListId).ToListAsync().ConfigureAwait(false);
            if (doDeleteFiles)
            {
                try
                {
                    Directory.Delete(release.ReleaseFileFolder(release.Artist.ArtistFileFolder(Configuration)), true);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
            DbContext.Releases.Remove(release);
            var i = await DbContext.SaveChangesAsync().ConfigureAwait(false);
            result = true;
            try
            {
                CacheManager.ClearRegion(releaseCacheRegion);
                CacheManager.ClearRegion(artistCacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error Clearing Cache For Release [{release.Id}] Exception [{ex.Serialize()}]");
            }

            var now = DateTime.UtcNow;
            if (doUpdateArtistCounts)
            {
                var releaseArtistIds = new List<int>(release.ArtistId);
                releaseArtistIds.AddRange(releaseTracks.Where(x => x.ArtistId.HasValue).Select(x => x.ArtistId.Value));
                foreach (var artistId in releaseArtistIds.Distinct())
                {
                    await UpdateArtistCounts(artistId, now).ConfigureAwait(false);
                }
            }
            if (releaseLabelIds?.Any() == true)
            {
                foreach (var releaseLabelId in releaseLabelIds)
                {
                    await UpdateLabelCounts(releaseLabelId, now).ConfigureAwait(false);
                }
            }
            sw.Stop();
            await BookmarkService.RemoveAllBookmarksForItemAsync(BookmarkType.Release, release.Id).ConfigureAwait(false);
            foreach (var releaseTrack in releaseTracks)
            {
                await BookmarkService.RemoveAllBookmarksForItemAsync(BookmarkType.Track, releaseTrack.Id).ConfigureAwait(false);
            }
            foreach (var playlistIdWithReleaseTracks in playlistIdsWithReleaseTracks)
            {
                await UpdatePlaylistCounts(playlistIdWithReleaseTracks, now).ConfigureAwait(false);
            }
            Logger.LogWarning("User `{0}` deleted Release `{1}]`", user, release);
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteReleasesAsync(Library.Identity.User user, IEnumerable<Guid> releaseIds, bool doDeleteFiles = false)
        {
            if (!releaseIds.Any())
            {
                return new OperationResult<bool>(true, "Invalid ReleaseIds");
            }
            var result = false;
            var sw = new Stopwatch();
            sw.Start();

            var now = DateTime.UtcNow;
            var releases = (from r in DbContext.Releases.Include(r => r.Artist)
                            where releaseIds.Contains(r.RoadieId)
                            select r
                ).ToArray();

            var artistIds = releases.Select(x => x.ArtistId).Distinct().ToArray();

            foreach (var release in releases)
            {
                var defaultResult = await DeleteAsync(user, release, doDeleteFiles, false).ConfigureAwait(false);
                result &= defaultResult.IsSuccess;
            }

            foreach (var artistId in artistIds)
            {
                await UpdateArtistCounts(artistId, now).ConfigureAwait(false);
            }
            sw.Stop();

            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<ReleaseList>> ListAsync(User roadieUser, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            int? rowCount = null;

            IQueryable<int> collectionReleaseIds = null;
            if (request.FilterToCollectionId.HasValue)
            {
                collectionReleaseIds = from cr in DbContext.CollectionReleases
                                       join c in DbContext.Collections on cr.CollectionId equals c.Id
                                       join r in DbContext.Releases on cr.ReleaseId equals r.Id
                                       where c.RoadieId == request.FilterToCollectionId.Value
                                       orderby cr.ListNumber
                                       select r.Id;
            }
            IQueryable<int> favoriteReleaseIds = null;
            if (request.FilterFavoriteOnly)
            {
                favoriteReleaseIds = from a in DbContext.Releases
                                     join ur in DbContext.UserReleases on a.Id equals ur.ReleaseId
                                     where ur.IsFavorite ?? false
                                     where roadieUser == null || ur.UserId == roadieUser.Id
                                     select a.Id;
            }
            IQueryable<int> genreReleaseIds = null;
            var isFilteredToGenre = false;
            if (request.FilterToGenreId.HasValue)
            {
                genreReleaseIds = (from rg in DbContext.ReleaseGenres
                                   join g in DbContext.Genres on rg.GenreId equals g.Id
                                   where g.RoadieId == request.FilterToGenreId
                                   select rg.ReleaseId)
                                   .Distinct();
                isFilteredToGenre = true;
            }
            else if (!string.IsNullOrEmpty(request.FilterByGenre) || (!string.IsNullOrEmpty(request.Filter) && request.Filter.StartsWith(":genre", StringComparison.OrdinalIgnoreCase)))
            {
                var genreFilter = request.FilterByGenre ?? (request.Filter ?? string.Empty).Replace(":genre ", string.Empty, StringComparison.OrdinalIgnoreCase);
                genreReleaseIds = (from rg in DbContext.ReleaseGenres
                                   join g in DbContext.Genres on rg.GenreId equals g.Id
                                   where g.Name.Contains(genreFilter)
                                   select rg.ReleaseId)
                                   .Distinct();
                request.Filter = null;
                isFilteredToGenre = true;
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
            var isEqualFilter = false;
            if (!string.IsNullOrEmpty(request.FilterValue))
            {
                var filter = request.FilterValue;
                // if filter string is wrapped in quotes then is an exact not like search, e.g. "Diana Ross" should not return "Diana Ross & The Supremes"
                if (filter.StartsWith('"') && filter.EndsWith('"'))
                {
                    isEqualFilter = true;
#pragma warning disable IDE0057 // Use range operator
                    request.Filter = filter.Substring(1, filter.Length - 2);
#pragma warning restore IDE0057 // Use range operator
                }
            }

            var normalizedFilterValue = !string.IsNullOrEmpty(request.FilterValue)
                ? request.FilterValue.ToAlphanumericName()
                : null;

            int[] randomReleaseIds = null;
            SortedDictionary<int, int> randomReleaseData = null;
            if (doRandomize ?? false)
            {
                var randomLimit = request.Limit ?? roadieUser?.RandomReleaseLimit ?? request.LimitValue;
                randomReleaseData = await DbContext.RandomReleaseIdsAsync(roadieUser?.Id ?? -1, randomLimit, request.FilterFavoriteOnly, request.FilterRatedOnly).ConfigureAwait(false);
                randomReleaseIds = randomReleaseData.Select(x => x.Value).ToArray();
                rowCount = DbContext.Releases.Count();
            }

            var result = from r in DbContext.Releases
                         join a in DbContext.Artists on r.ArtistId equals a.Id
                         where randomReleaseIds == null || randomReleaseIds.Contains(r.Id)
                         where request.FilterMinimumRating == null || r.Rating >= request.FilterMinimumRating.Value
                         where request.FilterToArtistId == null || r.Artist.RoadieId == request.FilterToArtistId
                         where request.FilterToCollectionId == null || collectionReleaseIds.Contains(r.Id)
                         where !request.FilterFavoriteOnly || favoriteReleaseIds.Contains(r.Id)
                         where !isFilteredToGenre || genreReleaseIds.Contains(r.Id)
                         where request.FilterFromYear == null ||
                               (r.ReleaseDate != null && r.ReleaseDate.Value.Year <= request.FilterFromYear)
                         where request.FilterToYear == null ||
                               (r.ReleaseDate != null && r.ReleaseDate.Value.Year >= request.FilterToYear)
                         where string.IsNullOrEmpty(normalizedFilterValue) ||
                               r.Title.ToLower().Contains(normalizedFilterValue) ||
                               r.AlternateNames.ToLower().Contains(normalizedFilterValue)
                         where !isEqualFilter ||
                               r.Title.ToLower().Equals(normalizedFilterValue) ||
                               r.AlternateNames.ToLower().Contains(normalizedFilterValue)
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
                             ArtistThumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, a.RoadieId),
                             CreatedDate = r.CreatedDate,
                             Duration = r.Duration,
                             LastPlayed = r.LastPlayed,
                             LastUpdated = r.LastUpdated,
                             LibraryStatus = r.LibraryStatus,
                             MediaCount = r.MediaCount,
                             Rating = r.Rating,
                             Rank = (double?)r.Rank,
                             ReleaseDateDateTime = r.ReleaseDate,
                             ReleasePlayUrl = $"{HttpContext.BaseUrl}/play/release/{r.RoadieId}",
                             Status = r.Status,
                             Thumbnail = ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, r.RoadieId),
                             TrackCount = r.TrackCount,
                             TrackPlayedCount = r.PlayedCount
                         };

            ReleaseList[] rows = null;

            rowCount ??= result.Count();

            if (doRandomize ?? false)
            {
                var resultData = await result.ToArrayAsync().ConfigureAwait(false);
                rows = (from r in resultData
                        join ra in randomReleaseData on r.DatabaseId equals ra.Value
                        orderby ra.Key
                        select r
                       ).ToArray();
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
                    sortBy = request.OrderValue(new Dictionary<string, string> { { "ReleaseDateDateTime", "ASC" }, { "Release.Text", "ASC" } });
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
                    rows = await result.ToArrayAsync().ConfigureAwait(false);
                }
                else
                {
                    sortBy = sortBy.Replace("ReleaseDate ", "ReleaseDateDateTime ");

                    rows = await result
                                .OrderBy(sortBy)
                                .Skip(request.SkipValue)
                                .Take(request.LimitValue)
                                .ToArrayAsync().ConfigureAwait(false);
                }
            }

            if (rows.Length > 0)
            {
                var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                var genreData = await (from rg in DbContext.ReleaseGenres
                                       join g in DbContext.Genres on rg.GenreId equals g.Id
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
                                       }).ToArrayAsync().ConfigureAwait(false);

                foreach (var release in rows)
                {
                    var genre = Array.Find(genreData, x => x.ReleaseId == release.DatabaseId);
                    release.Genre = genre?.dt ?? new DataToken();
                }

                if (request.FilterToCollectionId.HasValue)
                {
                    var newRows = new List<ReleaseList>(rows);
                    var collection = await GetCollection(request.FilterToCollectionId.Value).ConfigureAwait(false);
                    var collectionReleases = from c in DbContext.Collections
                                             join cr in DbContext.CollectionReleases on c.Id equals cr.CollectionId
                                             where c.RoadieId == request.FilterToCollectionId
                                             select cr;
                    foreach (var par in collection.PositionArtistReleases().ToArray())
                    {
                        var cr = collectionReleases.FirstOrDefault(x => x.ListNumber == par.Position);
                        // Release is known for Collection CSV
                        if (cr != null)
                        {
                            var parRelease = Array.Find(rows, x => x.DatabaseId == cr.ReleaseId);
                            if (parRelease?.ListNumber.HasValue == false)
                            {
                                parRelease.ListNumber = par.Position;
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
                                Status = Statuses.Missing,
                                ArtistThumbnail = ImageHelper.MakeUnknownImage(HttpContext),
                                Thumbnail = ImageHelper.MakeUnknownImage(HttpContext),
                                ListNumber = par.Position
                            });
                        }
                    }

                    // Resort the list for the collection by listNumber
                    if (request.FilterToStatusValue != Statuses.Ok)
                    {
                        newRows = newRows.Where(x => x.Status == request.FilterToStatusValue).ToList();
                    }
                    rows = newRows
                            .OrderBy(x => x.ListNumber)
                            .Skip(request.SkipValue)
                            .Take(request.LimitValue)
                            .ToArray();
                    rowCount = collection.CollectionCount;
                }

                if (roadieUser != null)
                {
                    var userReleaseRatings = await (from ur in DbContext.UserReleases
                                                    where ur.UserId == roadieUser.Id
                                                    where rowIds.Contains(ur.ReleaseId)
                                                    select ur).ToArrayAsync().ConfigureAwait(false);

                    foreach (var userReleaseRating in userReleaseRatings.Where(x =>
                        rows.Select(r => r.DatabaseId).Contains(x.ReleaseId)))
                    {
                        var row = Array.Find(rows, x => x.DatabaseId == userReleaseRating.ReleaseId);
                        if (row != null)
                        {
                            var isDisliked = userReleaseRating.IsDisliked ?? false;
                            var isFavorite = userReleaseRating.IsFavorite ?? false;
                            row.UserRating = new UserRelease
                            {
                                IsDisliked = isDisliked,
                                IsFavorite = isFavorite,
                                Rating = userReleaseRating.Rating,
                                RatedDate = isDisliked || isFavorite
                                    ? (DateTime?)(userReleaseRating.LastUpdated ?? userReleaseRating.CreatedDate)
                                    : null
                            };
                        }
                    }
                }
            }

            if (includes?.Any() == true && includes.Contains("tracks"))
            {
                var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                var userRatingsForResult = await (from ut in DbContext.UserTracks.Include(x => x.Track)
                                                  join t in DbContext.Tracks on ut.TrackId equals t.Id
                                                  join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                  where rowIds.Contains(rm.ReleaseId)
                                                  where ut.UserId == roadieUser.Id
                                                  select ut)
                                                 .ToArrayAsync().ConfigureAwait(false) ?? new data.UserTrack[0];

                foreach (var release in rows)
                {
                    release.Media = DbContext.ReleaseMedias
                        .Include(x => x.Tracks)
                        .Where(x => x.ReleaseId == release.DatabaseId)
                        .ToArray()
                        .AsQueryable()
                        .ProjectToType<ReleaseMediaList>()
                        .OrderBy(x => x.MediaNumber)
                        .ToArray(); // Async operation on Project Mapping async throws error

                    Parallel.ForEach(release.Media, (media) =>
                    {
                        media.Tracks = media.Tracks.OrderBy(x => x.TrackNumber);
                        var userTracksForMedia = userRatingsForResult.Where(x => x.Track.ReleaseMediaId == media.DatabaseId).ToArray();
                        if (userTracksForMedia.Length > 0)
                        {
                            Parallel.ForEach(userTracksForMedia, (userTrack) =>
                            {
                                var mediaTrack = media.Tracks.FirstOrDefault(x => x.DatabaseId == userTrack.TrackId);
                                if (mediaTrack != null)
                                {
                                    mediaTrack.UserRating = new UserTrack
                                    {
                                        Rating = userTrack.Rating,
                                        IsFavorite = userTrack.IsFavorite ?? false,
                                        IsDisliked = userTrack.IsDisliked ?? false
                                    };
                                }
                            });
                        }
                    });
                }
            }

            if (request.FilterFavoriteOnly)
            {
                rows = rows.OrderBy(x => x.UserRating.Rating).ToArray();
            }
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<ReleaseList>
            {
                TotalCount = rowCount.Value,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            };
        }

        public async Task<OperationResult<bool>> MergeReleasesAsync(Library.Identity.User user, Guid releaseToMergeId, Guid releaseToMergeIntoId, bool addAsMedia)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var releaseToMerge = DbContext.Releases
                .Include(x => x.Artist)
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .Include(x => x.Medias)
                .Include("Medias.Tracks")
                .Include("Medias.Tracks.TrackArtist")
                .FirstOrDefault(x => x.RoadieId == releaseToMergeId);
            if (releaseToMerge == null)
            {
                Logger.LogWarning("MergeReleases Unknown Release [{0}]", releaseToMergeId);
                return new OperationResult<bool>(true, $"Release Not Found [{releaseToMergeId}]");
            }

            var releaseToMergeInfo = DbContext.Releases
                .Include(x => x.Artist)
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .Include(x => x.Medias)
                .Include("Medias.Tracks")
                .Include("Medias.Tracks.TrackArtist")
                .FirstOrDefault(x => x.RoadieId == releaseToMergeIntoId);
            if (releaseToMergeInfo == null)
            {
                Logger.LogWarning("MergeReleases Unknown Release [{0}]", releaseToMergeIntoId);
                return new OperationResult<bool>(true, $"Release Not Found [{releaseToMergeIntoId}]");
            }

            try
            {
                await MergeReleasesAsync(user, releaseToMerge, releaseToMergeInfo, addAsMedia).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();
            Logger.LogInformation("MergeReleases Release `{0}` Merged Into Release `{1}`, By User `{2}`",
                releaseToMerge, releaseToMergeInfo, user);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        /// <summary>
        /// Merge one release into another one
        /// </summary>
        /// <param name="releaseToMerge">The release to be merged</param>
        /// <param name="releaseToMergeInto">The release to merge into</param>
        /// <param name="addAsMedia">If true then add a ReleaseMedia to the release to be merged into</param>
        public async Task<OperationResult<bool>> MergeReleasesAsync(Library.Identity.User user, data.Release releaseToMerge, data.Release releaseToMergeInto, bool addAsMedia)
        {
            SimpleContract.Requires<ArgumentNullException>(releaseToMerge != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(releaseToMergeInto != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(releaseToMerge.Artist != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentNullException>(releaseToMergeInto.Artist != null, "Invalid Artist");
            var result = false;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var mergedTracksToMove = new List<data.Track>();

                releaseToMergeInto.MediaCount ??= 0;

                var now = DateTime.UtcNow;
                var releaseToMergeReleaseMedia = DbContext.ReleaseMedias.Where(x => x.ReleaseId == releaseToMerge.Id).ToList();
                var releaseToMergeIntoReleaseMedia = DbContext.ReleaseMedias.Where(x => x.ReleaseId == releaseToMergeInto.Id).ToList();
                var releaseToMergeIntoLastMediaNumber = releaseToMergeIntoReleaseMedia.Max(x => x.MediaNumber);

                // Add new ReleaseMedia
                if (addAsMedia || releaseToMergeIntoReleaseMedia.Count == 0)
                {
                    foreach (var rm in releaseToMergeReleaseMedia)
                    {
                        releaseToMergeIntoLastMediaNumber++;
                        rm.ReleaseId = releaseToMergeInto.Id;
                        rm.MediaNumber = releaseToMergeIntoLastMediaNumber;
                        rm.LastUpdated = now;
                        releaseToMergeInto.MediaCount++;
                        releaseToMergeInto.TrackCount += rm.TrackCount;
                    }
                }
                // Merge into existing ReleaseMedia
                else
                {
                    // See if each media exists and merge details of each including tracks
                    foreach (var rm in releaseToMergeReleaseMedia)
                    {
                        var existingReleaseMedia = releaseToMergeIntoReleaseMedia.Find(x => x.MediaNumber == rm.MediaNumber);
                        var mergeTracks = DbContext.Tracks.Where(x => x.ReleaseMediaId == rm.Id).ToArray();
                        if (existingReleaseMedia == null)
                        {
                            releaseToMergeIntoLastMediaNumber++;
                            // Doesnt exist in release being merged to add
                            rm.ReleaseId = releaseToMergeInto.Id;
                            rm.MediaNumber = releaseToMergeIntoLastMediaNumber;
                            rm.LastUpdated = now;
                            releaseToMergeInto.MediaCount++;
                            releaseToMergeInto.TrackCount += rm.TrackCount;
                            mergedTracksToMove.AddRange(mergeTracks);
                        }
                        else
                        {
                            // ReleaseMedia Does exist merge tracks and details

                            var mergeIntoTracks = DbContext.Tracks.Where(x => x.ReleaseMediaId == existingReleaseMedia.Id).ToArray();
                            foreach (var mergeTrack in mergeTracks)
                            {
                                var existingTrack = Array.Find(mergeIntoTracks, x => x.TrackNumber == mergeTrack.TrackNumber);
                                if (existingTrack == null)
                                {
                                    // Track does not exist, update to existing ReleaseMedia and update ReleaseToMergeInfo counts
                                    mergeTrack.LastUpdated = now;
                                    mergeTrack.ReleaseMediaId = existingReleaseMedia.Id;
                                    existingReleaseMedia.TrackCount++;
                                    existingReleaseMedia.LastUpdated = now;
                                    releaseToMergeInto.TrackCount++;
                                    mergedTracksToMove.Add(mergeTrack);
                                }
                                else
                                {
                                    // Track does exist merge two tracks together
                                    existingTrack.MusicBrainzId ??= mergeTrack.MusicBrainzId;
                                    existingTrack.SpotifyId ??= mergeTrack.SpotifyId;
                                    existingTrack.AmgId ??= mergeTrack.AmgId;
                                    existingTrack.ISRC ??= mergeTrack.ISRC;
                                    existingTrack.AmgId ??= mergeTrack.AmgId;
                                    existingTrack.LastFMId ??= mergeTrack.LastFMId;
                                    existingTrack.PartTitles ??= mergeTrack.PartTitles;
                                    existingTrack.PlayedCount = (existingTrack.PlayedCount ?? 0) + (mergeTrack.PlayedCount ?? 0);
                                    if (mergeTrack.LastPlayed.HasValue && existingTrack.LastPlayed.HasValue &&
                                        mergeTrack.LastPlayed > existingTrack.LastPlayed)
                                    {
                                        existingTrack.LastPlayed = mergeTrack.LastPlayed;
                                    }

                                    existingTrack.MusicBrainzId ??= mergeTrack.MusicBrainzId;
                                    existingTrack.Tags = existingTrack.Tags.AddToDelimitedList(mergeTrack.Tags.ToListFromDelimited());
                                    if (!mergeTrack.Title.Equals(existingTrack.Title,
                                        StringComparison.OrdinalIgnoreCase))
                                    {
                                        existingTrack.AlternateNames =
                                            existingTrack.AlternateNames.AddToDelimitedList(new[]
                                                {mergeTrack.Title, mergeTrack.Title.ToAlphanumericName()});
                                    }

                                    existingTrack.AlternateNames = existingTrack.AlternateNames.AddToDelimitedList(mergeTrack.AlternateNames.ToListFromDelimited());
                                    existingTrack.LastUpdated = now;
                                }
                            }
                        }
                    }
                }

                var releaseToMergeFolder = releaseToMerge.ReleaseFileFolder(releaseToMerge.Artist.ArtistFileFolder(Configuration));
                var releaseToMergeIntoArtistFolder = releaseToMergeInto.Artist.ArtistFileFolder(Configuration);
                var releaseToMergeIntoDirectory = new DirectoryInfo(releaseToMergeInto.ReleaseFileFolder(releaseToMergeIntoArtistFolder));

                // Move tracks for releaseToMergeInto into correct folders
                if (mergedTracksToMove.Count > 0)
                {
                    foreach (var track in mergedTracksToMove)
                    {
                        var oldTrackPath = track.PathToTrack(Configuration);
                        var newTrackPath = FolderPathHelper.TrackFullPath(Configuration, releaseToMergeInto.Artist, releaseToMergeInto, track);
                        var trackFile = new FileInfo(oldTrackPath);
                        if (!newTrackPath.Equals(oldTrackPath, StringComparison.OrdinalIgnoreCase))
                        {
                            var audioMetaData = await AudioMetaDataHelper.GetInfo(trackFile).ConfigureAwait(false);
                            track.FilePath = FolderPathHelper.TrackPath(Configuration, releaseToMergeInto.Artist, releaseToMergeInto, track);
                            track.Hash = HashHelper.CreateMD5(releaseToMergeInto.ArtistId + trackFile.LastWriteTimeUtc.GetHashCode().ToString() + audioMetaData.GetHashCode());
                            track.LastUpdated = now;
                            File.Move(oldTrackPath, newTrackPath);

                            // If track has track image move into new folder
                            var trackImageFileName = track.PathToTrackThumbnail(Configuration);
                            if (File.Exists(trackImageFileName))
                            {
                                File.Move(trackImageFileName, newTrackPath);
                            }
                        }
                    }
                }

                // Cleanup folders
                FileDirectoryProcessorService.DeleteEmptyFolders(new DirectoryInfo(releaseToMergeIntoArtistFolder), Logger);

                // Now Merge release details
                releaseToMergeInto.AlternateNames = releaseToMergeInto.AlternateNames.AddToDelimitedList(new[]
                    {releaseToMerge.Title, releaseToMerge.Title.ToAlphanumericName()});
                releaseToMergeInto.AlternateNames =
                    releaseToMergeInto.AlternateNames.AddToDelimitedList(releaseToMerge.AlternateNames
                        .ToListFromDelimited());
                releaseToMergeInto.Tags =
                    releaseToMergeInto.Tags.AddToDelimitedList(releaseToMerge.Tags.ToListFromDelimited());
                releaseToMergeInto.URLs.AddToDelimitedList(releaseToMerge.URLs.ToListFromDelimited());
                releaseToMergeInto.MusicBrainzId ??= releaseToMerge.MusicBrainzId;
                releaseToMergeInto.Profile ??= releaseToMerge.Profile;
                releaseToMergeInto.ReleaseDate ??= releaseToMerge.ReleaseDate;
                releaseToMergeInto.MusicBrainzId ??= releaseToMerge.MusicBrainzId;
                releaseToMergeInto.DiscogsId ??= releaseToMerge.DiscogsId;
                releaseToMergeInto.ITunesId ??= releaseToMerge.ITunesId;
                releaseToMergeInto.AmgId ??= releaseToMerge.AmgId;
                releaseToMergeInto.LastFMId ??= releaseToMerge.LastFMId;
                releaseToMergeInto.LastFMSummary ??= releaseToMerge.LastFMSummary;
                releaseToMergeInto.SpotifyId ??= releaseToMerge.SpotifyId;
                if (releaseToMergeInto.ReleaseType == ReleaseType.Unknown &&
                    releaseToMerge.ReleaseType != ReleaseType.Unknown)
                {
                    releaseToMergeInto.ReleaseType = releaseToMerge.ReleaseType;
                }

                releaseToMergeInto.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                // Update any collection pointers for release to be merged
                var collectionRecords = DbContext.CollectionReleases.Where(x => x.ReleaseId == releaseToMerge.Id);
                if (collectionRecords?.Any() == true)
                {
                    foreach (var cr in collectionRecords)
                    {
                        cr.ReleaseId = releaseToMergeInto.Id;
                        cr.LastUpdated = now;
                    }

                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                }

                // Update any existing playlist for release to be merged
                var playListTrackInfos = (from pl in DbContext.PlaylistTracks
                                          join t in DbContext.Tracks on pl.TrackId equals t.Id
                                          join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                          where rm.ReleaseId == releaseToMerge.Id
                                          select new
                                          {
                                              track = t,
                                              rm,
                                              pl
                                          }).ToArray();
                if (playListTrackInfos?.Any() == true)
                {
                    foreach (var playListTrackInfo in playListTrackInfos)
                    {
                        var matchingTrack = await (from t in DbContext.Tracks
                                                   join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                   where rm.ReleaseId == releaseToMergeInto.Id
                                                   where rm.MediaNumber == playListTrackInfo.rm.MediaNumber
                                                   where t.TrackNumber == playListTrackInfo.track.TrackNumber
                                                   select t).FirstOrDefaultAsync().ConfigureAwait(false);
                        if (matchingTrack != null)
                        {
                            playListTrackInfo.pl.TrackId = matchingTrack.Id;
                            playListTrackInfo.pl.LastUpdated = now;
                        }
                    }

                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                }

                await DeleteAsync(user, releaseToMerge).ConfigureAwait(false);

                // Merge all release images
                var releaseCoverFiles = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(releaseToMergeFolder), ImageType.Release);
                if (releaseCoverFiles.Any())
                {
                    foreach (var releaseCoverFile in releaseCoverFiles)
                    {
                        var imagePath = Path.Combine(releaseToMergeIntoDirectory.FullName, releaseCoverFile.Name);
                        if (File.Exists(imagePath))
                        {
                            if (ImageHelper.IsImageBetterQuality(releaseCoverFile.FullName, imagePath))
                            {
                                releaseCoverFile.MoveTo(imagePath, true);
                            }
                        }
                        else
                        {
                            releaseCoverFile.MoveTo(imagePath);
                        }
                    }
                }
                var secondaryReleaseCoverFiles = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(releaseToMergeFolder), ImageType.ReleaseSecondary);
                if (secondaryReleaseCoverFiles.Any())
                {
                    foreach (var secondaryReleaseCoverFile in secondaryReleaseCoverFiles)
                    {
                        var imagePath = Path.Combine(releaseToMergeIntoDirectory.FullName, secondaryReleaseCoverFile.Name);
                        if (File.Exists(imagePath))
                        {
                            if (ImageHelper.IsImageBetterQuality(secondaryReleaseCoverFile.FullName, imagePath))
                            {
                                secondaryReleaseCoverFile.MoveTo(imagePath, true);
                            }
                        }
                        else
                        {
                            secondaryReleaseCoverFile.MoveTo(imagePath, true);
                        }
                    }
                }

                var releaseToMergeInfoDirectory = new DirectoryInfo(releaseToMergeFolder);
                if (releaseToMergeInfoDirectory.Exists)
                {
                    releaseToMergeInfoDirectory.Delete(true);
                    Logger.LogWarning("x Deleted Folder [{0}]", releaseToMergeInfoDirectory);
                }

                // Clear cache regions for manipulated records
                CacheManager.ClearRegion(releaseToMergeInto.CacheRegion);
                if (releaseToMergeInto.Artist != null)
                {
                    CacheManager.ClearRegion(releaseToMergeInto.Artist.CacheRegion);
                }

                if (releaseToMerge.Artist != null)
                {
                    CacheManager.ClearRegion(releaseToMerge.Artist.CacheRegion);
                }

                // Rescan release
                await ScanReleaseFolderAsync(user, releaseToMergeInto.RoadieId, false, releaseToMergeInto).ConfigureAwait(false);

                sw.Stop();
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"MergeReleases ReleaseToMerge `{releaseToMerge}`, ReleaseToMergeInto `{releaseToMergeInto}`, addAsMedia [{addAsMedia}]");
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

        public async Task<FileOperationResult<byte[]>> ReleaseZippedAsync(User roadieUser, Guid id)
        {
            var release = await GetRelease(id).ConfigureAwait(false);
            if (release == null)
            {
                return new FileOperationResult<byte[]>(true, $"Release Not Found [{id}]");
            }

            byte[] zipBytes = null;
            string zipFileName = null;
            try
            {
                var artistFolder = release.Artist.ArtistFileFolder(Configuration);
                var releaseFolder = release.ReleaseFileFolder(artistFolder);
                if (!Directory.Exists(releaseFolder))
                {
                    Logger.LogCritical($"Release Folder [{releaseFolder}] not found for Release `{release}`");
                    return new FileOperationResult<byte[]>(true, $"Release Folder Not Found [{id}]");
                }

                var releaseFiles = Directory.GetFiles(releaseFolder);
                using (var zipStream = new MemoryStream())
                {
                    using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        foreach (var releaseFile in releaseFiles)
                        {
                            var fileInfo = new FileInfo(releaseFile);
                            if (string.Equals(fileInfo.Extension, ".mp3", StringComparison.OrdinalIgnoreCase) || string.Equals(fileInfo.Extension, ".jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                var entry = zip.CreateEntry(fileInfo.Name);
                                using (var entryStream = entry.Open())
                                {
                                    using (var s = fileInfo.OpenRead())
                                    {
                                        s.CopyTo(entryStream);
                                    }
                                }
                            }
                        }
                    }

                    zipBytes = zipStream.ToArray();
                }

                zipFileName = $"{release.Artist.Name}_{release.SortTitleValue}.zip".ToFileNameFriendly();
                Logger.LogTrace(
                    $"User `{roadieUser}` downloaded Release `{release}` ZipFileName [{zipFileName}], Zip Size [{zipBytes?.Length}]");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating zip for Release `{0}`", release.ToString());
            }

            return new FileOperationResult<byte[]>
            {
                IsSuccess = zipBytes != null,
                Data = zipBytes,
                AdditionalData = new Dictionary<string, object> { { "ZipFileName", zipFileName } }
            };
        }

        /// <summary>
        ///     For the given ReleaseId, Scan folder adding new, removing not found and updating DB tracks for tracks found
        /// </summary>
        public async Task<OperationResult<bool>> ScanReleaseFolderAsync(Library.Identity.User user, Guid releaseId, bool doJustInfo, data.Release releaseToScan = null)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>((releaseId != Guid.Empty && releaseToScan == null) || releaseToScan != null, "Invalid ReleaseId");

            _addedTrackIds.Clear();

            var result = false;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            var modifiedRelease = false;
            string releasePath = null;
            try
            {
                var release = releaseToScan ?? DbContext.Releases
                                  .Include(x => x.Artist)
                                  .Include(x => x.Labels)
                                  .FirstOrDefault(x => x.RoadieId == releaseId);
                if (release == null)
                {
                    Logger.LogCritical("Unable To Find Release [{0}]", releaseId);
                    return new OperationResult<bool>();
                }
                var now = DateTime.UtcNow;

                // This is recorded from metadata and if set then used to gauage if the release is complete
                short? totalTrackCount = null;
                short totalMissingCount = 0;
                releasePath = release.ReleaseFileFolder(release.Artist.ArtistFileFolder(Configuration));
                var releaseDirectory = new DirectoryInfo(releasePath);
                data.Track firstTrack = null;
                if (!Directory.Exists(releasePath))
                {
                    // Get the First Track for the Release from DB (if any) and see if the path on it is set, if so rename to "correct" path
                    //  this happens with the logic change from folder naming and almost always case change which causes issues with Linux systems.
                    firstTrack = (from t in DbContext.Tracks
                                  join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                  where rm.ReleaseId == release.Id
                                  select t).FirstOrDefault();
                    if (firstTrack?.FilePath != null)
                    {
                        var trackPath = Path.GetDirectoryName(firstTrack.PathToTrack(Configuration));
                        if (Directory.Exists(trackPath) && !releasePath.Equals(trackPath))
                        {
                            Directory.Move(trackPath, releasePath);
                            var releaseTracks = (from t in DbContext.Tracks
                                                 join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                 where rm.ReleaseId == release.Id
                                                 select t).ToArray();
                            foreach (var releaseTrack in releaseTracks)
                            {
                                releaseTrack.FilePath = FolderPathHelper.TrackPath(Configuration, release.Artist, release, releaseTrack);
                                if (!releaseTrack.DoesFileForTrackExist(Configuration))
                                {
                                    releaseTrack.Hash = null;
                                    releaseTrack.Status = Statuses.Missing;
                                }
                                else
                                {
                                    releaseTrack.Hash = HashHelper.CreateMD5(release.RoadieId + releaseTrack.GetHashCode().ToString());
                                    releaseTrack.Status = Statuses.Ok;
                                }
                                releaseTrack.LastUpdated = now;
                            }
                            release.LastUpdated = now;
                            await DbContext.SaveChangesAsync().ConfigureAwait(false);
                            Logger.LogWarning($"Release `{ release }`: Updated Release Track Path From [{ trackPath }] to [{ releasePath }]");
                        }
                        else
                        {
                            Logger.LogWarning($"Unable To Find Release Folder [{releasePath}], and Track path [{ trackPath }] For Release `{release}`");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Unable To Find Release Folder [{releasePath}] For Release `{release}`");
                    }
                }

                #region Get Tracks for Release from DB and set as missing any not found in Folder

                foreach (var releaseMedia in DbContext.ReleaseMedias.Where(x => x.ReleaseId == release.Id).ToArray())
                {
                    var foundMissingTracks = false;
                    foreach (var existingTrack in DbContext.Tracks.Where(x => x.ReleaseMediaId == releaseMedia.Id).ToArray())
                    {
                        var trackPath = existingTrack.PathToTrack(Configuration);

                        if (!File.Exists(trackPath))
                        {
                            Logger.LogWarning("Track `{0}`, File [{1}] Not Found.", existingTrack.ToString(), trackPath);
                            if (!doJustInfo)
                            {
                                existingTrack.UpdateTrackMissingFile(now);
                                foundMissingTracks = true;
                                modifiedRelease = true;
                                totalMissingCount++;
                            }
                        }
                    }

                    if (foundMissingTracks)
                    {
                        await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                }

                #endregion Get Tracks for Release from DB and set as missing any not found in Folder

                #region Scan Folder and Add or Update Existing Tracks from Files

                var existingReleaseMedia = DbContext.ReleaseMedias.Include(x => x.Tracks)
                                                                  .Where(x => x.ReleaseId == release.Id)
                                                                  .ToList();
                var foundInFolderTracks = new List<data.Track>();
                short totalNumberOfTracksFound = 0;
                // This is the number of tracks metadata says the release should have (releaseMediaNumber, TotalNumberOfTracks)
                var releaseMediaTotalNumberOfTracks = new Dictionary<short, short?>();
                var releaseMediaTracksFound = new Dictionary<int, short>();
                if (Directory.Exists(releasePath))
                {
                    foreach (var file in releaseDirectory.GetFiles("*.mp3", SearchOption.AllDirectories))
                    {
                        int? trackArtistId = null;
                        string partTitles = null;
                        var audioMetaData = await AudioMetaDataHelper.GetInfo(file, doJustInfo).ConfigureAwait(false);
                        // This is the path for the new track not in the database but the found MP3 file to be added to library
                        var trackPath = FolderPathHelper.TrackPath(Configuration, release.Artist.Id, release.Artist.SortNameValue, release.SortTitleValue, release.ReleaseDate.Value, audioMetaData.Title, audioMetaData.TrackNumber.Value);

                        if (audioMetaData.IsValid)
                        {
                            var trackHash = HashHelper.CreateMD5(release.ArtistId + file.LastWriteTimeUtc.GetHashCode().ToString() + audioMetaData.GetHashCode());
                            totalNumberOfTracksFound++;
                            totalTrackCount ??= (short)(audioMetaData.TotalTrackNumbers ?? 0);
                            var releaseMediaNumber = (short)(audioMetaData.Disc ?? 1);
                            if (!releaseMediaTotalNumberOfTracks.ContainsKey(releaseMediaNumber))
                            {
                                releaseMediaTotalNumberOfTracks.Add(releaseMediaNumber, (short)(audioMetaData.TotalTrackNumbers ?? 0));
                            }
                            else
                            {
                                releaseMediaTotalNumberOfTracks[releaseMediaNumber] = releaseMediaTotalNumberOfTracks[releaseMediaNumber].TakeLarger((short)(audioMetaData.TotalTrackNumbers ?? 0));
                            }
                            var releaseMedia = existingReleaseMedia.Find(x => x.MediaNumber == releaseMediaNumber);
                            if (releaseMedia == null)
                            {
                                // New ReleaseMedia - Not Found In Database
                                releaseMedia = new data.ReleaseMedia
                                {
                                    ReleaseId = release.Id,
                                    Status = Statuses.Incomplete,
                                    MediaNumber = releaseMediaNumber
                                };
                                DbContext.ReleaseMedias.Add(releaseMedia);
                                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                                existingReleaseMedia.Add(releaseMedia);
                                modifiedRelease = true;
                            }
                            else
                            {
                                // Existing ReleaseMedia Found
                                releaseMedia.LastUpdated = now;
                            }

                            var track = releaseMedia.Tracks.FirstOrDefault(x => x.TrackNumber == audioMetaData.TrackNumber);
                            if (track == null)
                            {
                                // New Track - Not Found In Database
                                track = new data.Track
                                {
                                    Status = Statuses.New,
                                    FilePath = trackPath,
                                    FileName = file.Name,
                                    FileSize = (int)file.Length,
                                    Hash = trackHash,
                                    MusicBrainzId = audioMetaData.MusicBrainzId,
                                    AmgId = audioMetaData.AmgId,
                                    SpotifyId = audioMetaData.SpotifyId,
                                    Title = audioMetaData.Title,
                                    TrackNumber = audioMetaData.TrackNumber ?? totalNumberOfTracksFound,
                                    Duration = audioMetaData.Time != null
                                        ? (int)audioMetaData.Time.Value.TotalMilliseconds
                                        : 0,
                                    ReleaseMediaId = releaseMedia.Id,
                                    ISRC = audioMetaData.ISRC,
                                    LastFMId = audioMetaData.LastFmId
                                };

                                if (audioMetaData.TrackArtist != null)
                                {
                                    if (audioMetaData.TrackArtists.Count() == 1)
                                    {
                                        var trackArtistData = await ArtistLookupEngine.GetByName(new AudioMetaData { Artist = audioMetaData.TrackArtist }, true).ConfigureAwait(false);
                                        if (trackArtistData.IsSuccess && release.ArtistId != trackArtistData.Data.Id)
                                        {
                                            trackArtistId = trackArtistData.Data.Id;
                                        }
                                        if (release.ArtistId == trackArtistData.Data.Id)
                                        {
                                            trackArtistId = null;
                                        }
                                    }
                                    else if (audioMetaData.TrackArtists.Any())
                                    {
                                        partTitles = string.Join(AudioMetaData.ArtistSplitCharacter.ToString(), audioMetaData.TrackArtists);
                                    }
                                    else
                                    {
                                        partTitles = audioMetaData.TrackArtist;
                                    }
                                }

                                var alt = track.Title.ToAlphanumericName();
                                track.AlternateNames = !alt.Equals(audioMetaData.Title, StringComparison.OrdinalIgnoreCase)
                                        ? track.AlternateNames.AddToDelimitedList(new[] { alt })
                                        : null;
                                track.ArtistId = trackArtistId;
                                track.PartTitles = partTitles;
                                DbContext.Tracks.Add(track);
                                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                                _addedTrackIds.Add(track.Id);
                                modifiedRelease = true;
                            }
                            else if (string.IsNullOrEmpty(track.Hash) || trackHash != track.Hash)
                            {
                                if (audioMetaData.TrackArtist != null)
                                {
                                    if (audioMetaData.TrackArtists.Count() == 1)
                                    {
                                        var trackArtistData = await ArtistLookupEngine.GetByName(new AudioMetaData { Artist = audioMetaData.TrackArtist }, true).ConfigureAwait(false);
                                        if (trackArtistData.IsSuccess && release.ArtistId != trackArtistData.Data.Id)
                                        {
                                            trackArtistId = trackArtistData.Data.Id;
                                        }
                                    }
                                    else if (audioMetaData.TrackArtists.Any())
                                    {
                                        partTitles = string.Join(AudioMetaData.ArtistSplitCharacter.ToString(), audioMetaData.TrackArtists);
                                    }
                                    else
                                    {
                                        partTitles = audioMetaData.TrackArtist;
                                    }
                                }

                                track.Title = audioMetaData.Title;
                                track.Duration = audioMetaData.Time != null
                                    ? (int)audioMetaData.Time.Value.TotalMilliseconds
                                    : 0;
                                track.TrackNumber = audioMetaData.TrackNumber ?? totalNumberOfTracksFound;
                                track.ArtistId = trackArtistId;
                                track.PartTitles = partTitles;
                                track.Hash = trackHash;
                                track.FileName = file.Name;
                                track.FileSize = (int)file.Length;
                                track.FilePath = trackPath;
                                track.Status = Statuses.Ok;
                                track.LastUpdated = now;
                                var alt = track.Title.ToAlphanumericName();
                                if (!alt.Equals(track.Title, StringComparison.OrdinalIgnoreCase))
                                {
                                    track.AlternateNames = track.AlternateNames.AddToDelimitedList(new[] { alt });
                                }
                                track.TrackNumber = audioMetaData.TrackNumber ?? -1;
                                track.LastUpdated = now;
                                modifiedRelease = true;
                            }
                            else if (track.Status != Statuses.Ok)
                            {
                                track.Status = Statuses.Ok;
                                track.LastUpdated = now;
                                modifiedRelease = true;
                            }

                            foundInFolderTracks.Add(track);
                            if (releaseMediaTracksFound.ContainsKey(releaseMedia.Id))
                            {
                                releaseMediaTracksFound[releaseMedia.Id]++;
                            }
                            else
                            {
                                releaseMediaTracksFound[releaseMedia.Id] = 1;
                            }
                        }
                        else
                        {
                            Logger.LogWarning("Release Track File Has Invalid MetaData `{0}`", audioMetaData.ToString());
                        }
                    }
                }
                else
                {
                    Logger.LogWarning("Unable To Find Releaes Path [{0}] For Release `{1}`", releasePath, release.ToString());
                }
                var releaseMediaNumbersFound = new List<short?>();
                foreach (var kp in releaseMediaTracksFound)
                {
                    var releaseMedia = DbContext.ReleaseMedias.FirstOrDefault(x => x.Id == kp.Key);
                    if (releaseMedia != null)
                    {
                        if (!releaseMediaNumbersFound.Any(x => x == releaseMedia.MediaNumber))
                        {
                            releaseMediaNumbersFound.Add(releaseMedia.MediaNumber);
                        }
                        var releaseMediaFoundInFolderTrackNumbers = foundInFolderTracks
                            .Where(x => x.ReleaseMediaId == releaseMedia.Id).Select(x => x.TrackNumber).OrderBy(x => x)
                            .ToArray();
                        var areTracksForRelaseMediaSequential = releaseMediaFoundInFolderTrackNumbers.Zip(releaseMediaFoundInFolderTrackNumbers.Skip(1), (a, b) => a + 1 == b).All(x => x);
                        if (!areTracksForRelaseMediaSequential)
                        {
                            Logger.LogTrace($"ReleaseMedia [{ releaseMedia.Id }] Track Numbers Are Not Sequential [{ string.Join(",", releaseMediaFoundInFolderTrackNumbers) }]");
                        }
                        releaseMedia.TrackCount = kp.Value;
                        releaseMedia.LastUpdated = now;
                        releaseMedia.Status = areTracksForRelaseMediaSequential ? Statuses.Ok : Statuses.Incomplete;
                        await DbContext.SaveChangesAsync().ConfigureAwait(false);
                        modifiedRelease = true;
                    }
                }

                var foundInFolderTrackNumbers = foundInFolderTracks.Select(x => x.TrackNumber).OrderBy(x => x).ToArray();
                if (modifiedRelease || !foundInFolderTrackNumbers.Length.Equals(release.TrackCount) ||
                    releaseMediaNumbersFound.Count != (release.MediaCount ?? 0))
                {
                    var areTracksForRelaseSequential = foundInFolderTrackNumbers.Zip(foundInFolderTrackNumbers.Skip(1), (a, b) => a + 1 == b).All(x => x);
                    var maxFoundInFolderTrackNumbers = foundInFolderTrackNumbers.Length > 0 ? foundInFolderTrackNumbers.Max() : (short)0;
                    release.Status = areTracksForRelaseSequential ? Statuses.Ok : Statuses.Incomplete;
                    release.TrackCount = (short)foundInFolderTrackNumbers.Length;
                    release.MediaCount = (short)releaseMediaNumbersFound.Count;
                    if (release.TrackCount < maxFoundInFolderTrackNumbers)
                    {
                        release.TrackCount = maxFoundInFolderTrackNumbers;
                    }
                    release.LibraryStatus = release.TrackCount > 0 && release.TrackCount == totalNumberOfTracksFound
                        ? LibraryStatus.Complete
                        : LibraryStatus.Incomplete;
                    release.LastUpdated = now;
                    release.Status = release.LibraryStatus == LibraryStatus.Complete
                        ? Statuses.Complete
                        : Statuses.Incomplete;

                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    CacheManager.ClearRegion(release.Artist.CacheRegion);
                    CacheManager.ClearRegion(release.CacheRegion);
                }

                #endregion Scan Folder and Add or Update Existing Tracks from Files

                var releaseCoverImage = ImageHelper.FindImageTypeInDirectory(releaseDirectory, ImageType.Release).FirstOrDefault();
                var doesReleaseHaveCoverImage = releaseCoverImage != null;
                if (doesReleaseHaveCoverImage)
                {
                    // See if image file is valid image if not delete it
                    if (ImageHelper.ConvertToJpegFormat(File.ReadAllBytes(releaseCoverImage.FullName)) == null)
                    {
                        releaseCoverImage.Delete();
                        doesReleaseHaveCoverImage = false;
                        Logger.LogWarning($"Deleted invalid cover image for Release `{ release }`");
                    }
                }
                if (!doesReleaseHaveCoverImage)
                {
                    // Since no release image found see if first track has image in metadata if so then extract to cover file
                    firstTrack = (from t in DbContext.Tracks
                                  join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                  where rm.ReleaseId == release.Id
                                  orderby rm.MediaNumber, t.TrackNumber
                                  select t).FirstOrDefault();
                    if (firstTrack?.FilePath != null)
                    {
                        var metaData = await AudioMetaDataHelper.GetInfo(new FileInfo(firstTrack.PathToTrack(Configuration)), doJustInfo).ConfigureAwait(false);
                        if (metaData.Images?.Any() == true)
                        {
                            var imageBytes = ImageHelper.ConvertToJpegFormat(metaData.Images.FirstOrDefault(x => x.Data != null)?.Data);
                            if (imageBytes != null)
                            {
                                var releaseImageFilename = Path.Combine(releasePath, ImageHelper.ReleaseCoverFilename);
                                File.WriteAllBytes(releaseImageFilename, imageBytes);
                                Logger.LogInformation($"Extracted cover image from track for Release `{ release}`");
                            }
                        }
                    }
                }

                sw.Stop();

                await UpdateReleaseCounts(release.Id, now).ConfigureAwait(false);
                await UpdatePlaylistCountsForRelease(release.Id, now).ConfigureAwait(false);
                await UpdateArtistCountsForRelease(release.Id, now).ConfigureAwait(false);
                await UpdateReleaseRank(release.Id).ConfigureAwait(false);

                CacheManager.ClearRegion(release.Artist.CacheRegion);
                CacheManager.ClearRegion(release.CacheRegion);

                if (release.Labels?.Any() == true)
                {
                    foreach (var label in release.Labels)
                    {
                        await UpdateLabelCounts(label.Id, now).ConfigureAwait(false);
                    }
                }
                Logger.LogInformation("Scanned Release `{0}` Folder [{1}], Modified Release [{2}], OperationTime [{3}]", release.ToString(), releasePath, modifiedRelease, sw.ElapsedMilliseconds);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ReleasePath [{releasePath}] {ex.Serialize()}");
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

        public async Task<OperationResult<Image>> SetReleaseImageByUrlAsync(Library.Identity.User user, Guid id, string imageUrl) => await SaveImageBytes(user, id, await WebHelper.BytesForImageUrl(HttpClientFactory, imageUrl).ConfigureAwait(false)).ConfigureAwait(false);

        public async Task<OperationResult<bool>> UpdateReleaseAsync(Library.Identity.User user, Release model, string originalReleaseFolder = null)
        {
            var didChangeArtist = false;
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var release = await DbContext.Releases
                .Include(x => x.Artist)
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .Include(x => x.Labels)
                .Include("Labels.Label")
                .FirstOrDefaultAsync(x => x.RoadieId == model.Id)
                .ConfigureAwait(false);
            if (release == null)
            {
                return new OperationResult<bool>(true, $"Release Not Found [{model.Id}]");
            }
            // If release is being renamed, see if release already exists for artist with new model supplied name
            var releaseTitle = release.SortTitleValue;
            var releaseModelTitle = model.SortTitleValue;
            if (releaseTitle.ToAlphanumericName() != releaseModelTitle.ToAlphanumericName())
            {
                var existingRelease = await DbContext.Releases.FirstOrDefaultAsync(x => x.Id != release.Id &&
                                                                             (x.Title == releaseModelTitle || x.SortTitle == releaseModelTitle) &&
                                                                             x.ArtistId == release.ArtistId).ConfigureAwait(false);
                if (existingRelease != null)
                {
                    return new OperationResult<bool>($"Release already exists `{ existingRelease }` for Artist with title [{ releaseModelTitle }].");
                }
            }
            try
            {
                var now = DateTime.UtcNow;
                var artistFolder = release.Artist.ArtistFileFolder(Configuration);
                originalReleaseFolder ??= release.ReleaseFileFolder(artistFolder);
                release.IsLocked = model.IsLocked;
                release.IsVirtual = model.IsVirtual;
                release.Status = SafeParser.ToEnum<Statuses>(model.Status);
                release.Title = model.Title;
                release.SortTitle = model.SortTitle;
                var specialReleaseTitle = model.Title.ToAlphanumericName();
                var alt = new List<string>(model.AlternateNamesList);
                if (!model.AlternateNamesList.Contains(specialReleaseTitle, StringComparer.OrdinalIgnoreCase))
                {
                    alt.Add(specialReleaseTitle);
                }
                release.AlternateNames = alt.ToDelimitedList();
                release.ReleaseDate = model.ReleaseDate;
                release.Rating = model.Rating;
                release.TrackCount = model.TrackCount;
                release.MediaCount = model.MediaCount;
                release.Profile = model.Profile;
                release.DiscogsId = model.DiscogsId;
                release.ReleaseType = SafeParser.ToEnum<ReleaseType>(model.ReleaseType);
                release.LibraryStatus = SafeParser.ToEnum<LibraryStatus>(model.LibraryStatus);
                release.ITunesId = model.ITunesId;
                release.AmgId = model.AmgId;
                release.LastFMId = model.LastFMId;
                release.LastFMSummary = model.LastFMSummary;
                release.MusicBrainzId = model.MusicBrainzId;
                release.SpotifyId = model.SpotifyId;
                release.Tags = model.TagsList.ToDelimitedList();
                release.URLs = model.URLsList.ToDelimitedList();

                data.Artist artist = null;
                if (model?.Artist?.Artist?.Value != null)
                {
                    artist = await DbContext.Artists.FirstOrDefaultAsync(x => x.RoadieId == SafeParser.ToGuid(model.Artist.Artist.Value)).ConfigureAwait(false);
                    if (artist != null && release.ArtistId != artist.Id)
                    {
                        release.ArtistId = artist.Id;
                        didChangeArtist = true;
                    }
                }
                var releaseImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (releaseImage != null)
                {
                    // Save unaltered image to cover file
                    var coverFileName = Path.Combine(release.ReleaseFileFolder(release.Artist.ArtistFileFolder(Configuration)), "cover.jpg");
                    File.WriteAllBytes(coverFileName, ImageHelper.ConvertToJpegFormat(releaseImage));
                }
                var releaseFolder = release.ReleaseFileFolder(release.Artist.ArtistFileFolder(Configuration));
                if (model.NewSecondaryImagesData?.Any() == true)
                {
                    // Additional images to add to artist
                    var looper = 0;
                    foreach (var newSecondaryImageData in model.NewSecondaryImagesData)
                    {
                        var releaseSecondaryImage = ImageHelper.ImageDataFromUrl(newSecondaryImageData);
                        if (releaseSecondaryImage != null)
                        {
                            // Ensure is jpeg first
                            releaseSecondaryImage = ImageHelper.ConvertToJpegFormat(releaseSecondaryImage);

                            var releaseImageFilename = Path.Combine(releaseFolder,
                                string.Format(ImageHelper.ReleaseSecondaryImageFilename, looper.ToString("00")));
                            while (File.Exists(releaseImageFilename))
                            {
                                looper++;
                                releaseImageFilename = Path.Combine(releaseFolder,
                                    string.Format(ImageHelper.ReleaseSecondaryImageFilename, looper.ToString("00")));
                            }

                            File.WriteAllBytes(releaseImageFilename, releaseSecondaryImage);
                        }

                        looper++;
                    }
                }

                if (model.Genres?.Any() == true)
                {
                    // Remove existing Genres not in model list
                    foreach (var genre in release.Genres.ToList())
                    {
                        var doesExistInModel = model.Genres.Any(x => SafeParser.ToGuid(x.Genre.Value) == genre.Genre.RoadieId);
                        if (!doesExistInModel)
                        {
                            release.Genres.Remove(genre);
                        }
                    }

                    // Add new Genres in model not in data
                    foreach (var genre in model.Genres)
                    {
                        var genreId = SafeParser.ToGuid(genre.Genre.Value);
                        var doesExistInData = release.Genres.Any(x => x.Genre.RoadieId == genreId);
                        if (!doesExistInData)
                        {
                            var g = DbContext.Genres.FirstOrDefault(x => x.RoadieId == genreId);
                            if (g != null)
                            {
                                release.Genres.Add(new data.ReleaseGenre
                                {
                                    ReleaseId = release.Id,
                                    GenreId = g.Id,
                                    Genre = g
                                });
                            }
                        }
                    }
                }
                else if (model.Genres?.Any() != true)
                {
                    release.Genres.Clear();
                }

                if (model.Labels?.Any() != true)
                {
                    // Delete all existing labels for release
                    var releaseLabelsToDelete = await (from l in DbContext.ReleaseLabels
                                                       where l.ReleaseId == release.Id
                                                       select l)
                                                       .ToArrayAsync()
                                                       .ConfigureAwait(false);
                    DbContext.ReleaseLabels.RemoveRange(releaseLabelsToDelete);
                }
                else if (model.Labels?.Any() == true)
                {
                    var releaseLabelIds = model.Labels.Select(x => x.Id).ToArray();
                    // Delete any labels not in given model (removed by edit operation)
                    var releaseLabelsToDelete = await (from l in DbContext.ReleaseLabels
                                                       where l.ReleaseId == release.Id
                                                       where !releaseLabelIds.Contains(l.RoadieId)
                                                       select l)
                                                       .ToArrayAsync()
                                                       .ConfigureAwait(false);
                    DbContext.ReleaseLabels.RemoveRange(releaseLabelsToDelete);
                    // Update any existing
                    foreach (var label in model.Labels)
                    {
                        var trackLabel = await DbContext.ReleaseLabels.FirstOrDefaultAsync(x => x.RoadieId == label.Id).ConfigureAwait(false);
                        if (trackLabel == null)
                        {
                            // Add new
                            trackLabel = new data.ReleaseLabel
                            {
                                ReleaseId = release.Id,
                                CreatedDate = now
                            };
                            DbContext.ReleaseLabels.Add(trackLabel);
                        }
                        trackLabel.CatalogNumber = label.CatalogNumber;
                        trackLabel.BeginDate = label.BeginDate;
                        trackLabel.EndDate = label.EndDate;
                        var releaseLabel = await GetLabel(SafeParser.ToGuid(label.Label.Label.Value).Value).ConfigureAwait(false);
                        trackLabel.LabelId = releaseLabel.Id;
                        trackLabel.IsLocked = label.IsLocked;
                        trackLabel.Status = SafeParser.ToEnum<Statuses>(label.Status);
                        trackLabel.LastUpdated = now;
                    }
                }

                if (model.Credits?.Any() != true)
                {
                    // Delete all existing credits for release
                    var releaseCreditsToDelete = await (from c in DbContext.Credits
                                                        where c.ReleaseId == release.Id
                                                        select c)
                                                        .ToArrayAsync()
                                                        .ConfigureAwait(false);
                    DbContext.Credits.RemoveRange(releaseCreditsToDelete);
                }
                else if (model.Credits?.Any() == true)
                {
                    var releaseCreditIds = model.Credits.Select(x => x.Id).ToArray();
                    // Delete any credits not given in model (removed by edit operation)
                    var releaseCreditsToDelete = await (from c in DbContext.Credits
                                                        where c.TrackId == release.Id
                                                        where !releaseCreditIds.Contains(c.RoadieId)
                                                        select c)
                                                        .ToArrayAsync()
                                                        .ConfigureAwait(false);
                    DbContext.Credits.RemoveRange(releaseCreditsToDelete);
                    // Update any existing
                    foreach (var credit in model.Credits)
                    {
                        var trackCredit = await DbContext.Credits.FirstOrDefaultAsync(x => x.RoadieId == credit.Id).ConfigureAwait(false);
                        if (trackCredit == null)
                        {
                            // Add new
                            trackCredit = new data.Credit
                            {
                                ReleaseId = release.Id,
                                CreatedDate = now
                            };
                            DbContext.Credits.Add(trackCredit);
                        }
                        data.Artist artistForCredit = null;
                        if (credit.Artist != null)
                        {
                            artistForCredit = await GetArtist(credit.Artist.Id).ConfigureAwait(false);
                        }
                        var creditCategory = await DbContext.CreditCategory.FirstOrDefaultAsync(x => x.RoadieId.ToString() == credit.Category.Value).ConfigureAwait(false);
                        trackCredit.CreditCategoryId = creditCategory.Id;
                        trackCredit.ArtistId = artistForCredit == null ? null : (int?)artistForCredit.Id;
                        trackCredit.IsLocked = credit.IsLocked;
                        trackCredit.Status = SafeParser.ToEnum<Statuses>(credit.Status);
                        trackCredit.CreditToName = artistForCredit == null ? credit.CreditName : null;
                        trackCredit.Description = credit.Description;
                        trackCredit.URLs = credit.URLs;
                        trackCredit.Tags = credit.Tags;
                        trackCredit.LastUpdated = now;
                    }
                }

                release.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                await CheckAndChangeReleaseTitle(release, originalReleaseFolder).ConfigureAwait(false);
                // Update release ID3 Tags
                foreach (var mp3 in Directory.GetFiles(releaseFolder, "*.mp3", SearchOption.AllDirectories))
                {
                    var trackFileInfo = new FileInfo(mp3);
                    var audioMetaData = await AudioMetaDataHelper.GetInfo(trackFileInfo).ConfigureAwait(false);
                    if (audioMetaData != null)
                    {
                        audioMetaData.Year = release.ReleaseYear;
                        audioMetaData.Release = release.Title;
                        if (artist != null)
                        {
                            audioMetaData.Artist = artist.Name;
                        }
                        AudioMetaDataHelper.WriteTags(audioMetaData, trackFileInfo);
                    }
                }
                CacheManager.ClearRegion(release.CacheRegion);
                Logger.LogInformation($"UpdateRelease `{release}` By User `{user}`: Edited Artist [{didChangeArtist}]");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<Image>> UploadReleaseImageAsync(Library.Identity.User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }

            return await SaveImageBytes(user, id, bytes).ConfigureAwait(false);
        }
    }
}