using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class ImageService : ServiceBase, IImageService
    {
        private IDefaultNotFoundImages DefaultNotFoundImages { get; }

        private IImageSearchManager ImageSearchManager { get; }

        public string Referrer { get; set; }
        public string RequestIp { get; set; }

        public ImageService(IRoadieSettings configuration, IRoadieDbContext dbContext, ICacheManager cacheManager,
                            ILogger logger, DefaultNotFoundImages defaultNotFoundImages)
            : base(configuration, null, dbContext, cacheManager, logger, null)
        {
            DefaultNotFoundImages = defaultNotFoundImages;
        }

        public ImageService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<ImageService> logger,
            IDefaultNotFoundImages defaultNotFoundImages,
            IImageSearchManager imageSearchManager)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            DefaultNotFoundImages = defaultNotFoundImages;
            ImageSearchManager = imageSearchManager;
        }

        /// <summary>
        /// Get image for an artist, see if the artist has an image in their folder and use that else use Artist.Thumbnail, is also not found use Artist DefaultNotFound image.
        /// </summary>
        private async Task<FileOperationResult<IImage>> ArtistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = await GetArtist(id).ConfigureAwait(false);
                if (artist == null)
                {
                    return new FileOperationResult<IImage>(true, $"Artist Not Found [{id}]");
                }
                byte[] imageBytes = null;
                string artistFolder = null;
                try
                {
                    // See if artist images exists in artist folder
                    artistFolder = artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Logger.LogWarning($"Artist Folder [{artistFolder}], Not Found For Artist `{artist}`");
                    }
                    else
                    {
                        var artistImages = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistFolder), ImageType.Artist);
                        if (artistImages.Any())
                        {
                            imageBytes = File.ReadAllBytes(artistImages.First().FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Folder [{artistFolder}] For Artist [{artist.Id}]");
                }

                IImage image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
                };
                if (imageBytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.Artist;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Artist Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<IImage>> ArtistSecondaryImageAction(Guid id, int imageId, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = await GetArtist(id).ConfigureAwait(false);
                if (artist == null)
                {
                    return new FileOperationResult<IImage>(true, $"Release Not Found [{id}]");
                }
                byte[] imageBytes = null;
                string artistFolder = null;
                try
                {
                    // See if cover art file exists in release folder
                    artistFolder = artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Logger.LogWarning($"Artist Folder [{artistFolder}], Not Found For Artist `{artist}`");
                    }
                    else
                    {
                        var artistSecondaryImages = ImageHelper
                            .FindImageTypeInDirectory(new DirectoryInfo(artistFolder), ImageType.ArtistSecondary)
                            .ToArray();
                        if (artistSecondaryImages.Length >= imageId && artistSecondaryImages[imageId] != null)
                        {
                            imageBytes = File.ReadAllBytes(artistSecondaryImages[imageId].FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Artist Folder [{artistFolder}] For Artist `{artist}`");
                }

                var image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
                    CreatedDate = artist.CreatedDate
                };
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<IImage>> CollectionImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var collection = await GetCollection(id).ConfigureAwait(false);
                if (collection == null)
                {
                    return new FileOperationResult<IImage>(true, $"Collection Not Found [{id}]");
                }
                IImage image = new Library.Imaging.Image(id)
                {
                    CreatedDate = collection.CreatedDate
                };
                var collectionImageFilename = collection.PathToImage(Configuration);
                try
                {
                    if (File.Exists(collectionImageFilename))
                    {
                        image.Bytes = File.ReadAllBytes(collectionImageFilename);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Image File [{collectionImageFilename}]");
                }
                if (image.Bytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.Collection;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Collection Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private FileOperationResult<IImage> GenerateFileOperationResult(Guid id, IImage image, EntityTagHeaderValue etag = null, string contentType = "image/jpeg")
        {
            var imageEtag = EtagHelper.GenerateETag(HttpEncoder, image.Bytes);
            if (EtagHelper.CompareETag(HttpEncoder, etag, imageEtag))
            {
                return new FileOperationResult<IImage>(OperationMessages.NotModified);
            }
            if (image?.Bytes?.Any() != true)
            {
                return new FileOperationResult<IImage>($"ImageById Not Set [{id}]");
            }
            return new FileOperationResult<IImage>(image?.Bytes?.Any() ?? false
                ? OperationMessages.OkMessage
                : OperationMessages.NoImageDataFound)
            {
                IsSuccess = true,
                Data = image,
                ContentType = contentType,
                LastModified = image.CreatedDate,
                ETag = imageEtag
            };
        }

        private async Task<FileOperationResult<IImage>> GenreImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var genre = await GetGenre(id).ConfigureAwait(false);
                if (genre == null)
                {
                    return new FileOperationResult<IImage>(true, $"Genre Not Found [{id}]");
                }
                IImage image = new Library.Imaging.Image(id)
                {
                    CreatedDate = genre.CreatedDate
                };
                var genreImageFilename = genre.PathToImage(Configuration);
                try
                {
                    if (File.Exists(genreImageFilename))
                    {
                        image.Bytes = File.ReadAllBytes(genreImageFilename);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Image File [{genreImageFilename}]");
                }
                if (image.Bytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.Genre;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Label Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<IImage>> GetImageFileOperation(string type, string regionUrn, Guid id, int? width, int? height, Func<Task<FileOperationResult<IImage>>> action, EntityTagHeaderValue etag = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var sizeHash = (width ?? 0) + (height ?? 0);
                var result = await CacheManager.GetAsync($"urn:{type}_by_id_operation:{id}:{sizeHash}", action, regionUrn).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return new FileOperationResult<IImage>(result.IsNotFoundResult, result.Messages);
                }
                if (result.ETag == etag && etag != null)
                {
                    return new FileOperationResult<IImage>(OperationMessages.NotModified);
                }
                var force = width.HasValue || height.HasValue;
                var newWidth = width ?? Configuration.MaximumImageSize.Width;
                var newHeight = height ?? Configuration.MaximumImageSize.Height;
                if (result?.Data?.Bytes != null)
                {
                    var resized = ImageHelper.ResizeImage(result?.Data?.Bytes, newWidth, newHeight, force);
                    if (resized != null)
                    {
                        result.Data.Bytes = resized.Item2;
                        result.ETag = EtagHelper.GenerateETag(HttpEncoder, result.Data.Bytes);
                        result.LastModified = DateTime.UtcNow;
                        if (resized.Item1)
                        {
                            Logger.LogTrace($"{type}: Resized [{id}], Width [{ newWidth}], Height [{ newHeight}], Forced [{ force }]");
                        }
                    }
                    else
                    {
                        Logger.LogTrace($"{type}: Image [{id}] Request returned Null Image, Forced [{ force }]");
                    }
                }

                sw.Stop();
                return new FileOperationResult<IImage>(result.Messages)
                {
                    Data = result.Data,
                    ETag = result.ETag,
                    LastModified = result.LastModified,
                    ContentType = result.ContentType,
                    Errors = result?.Errors,
                    IsSuccess = result?.IsSuccess ?? false,
                    OperationTime = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"GetImageFileOperation Error, Type [{type}], id [{id}]");
            }

            return new FileOperationResult<IImage>("System Error");
        }

        private async Task<FileOperationResult<IImage>> LabelImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var label = await GetLabel(id).ConfigureAwait(false);
                if (label == null)
                {
                    return new FileOperationResult<IImage>(true, $"Label Not Found [{id}]");
                }
                IImage image = new Library.Imaging.Image(id)
                {
                    CreatedDate = label.CreatedDate
                };
                var labelImageFilename = label.PathToImage(Configuration);
                try
                {
                    if (File.Exists(labelImageFilename))
                    {
                        image.Bytes = File.ReadAllBytes(labelImageFilename);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Image File [{labelImageFilename}]");
                }
                if (image.Bytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.Label;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Label Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<IImage>> PlaylistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                string playlistImageFilename = null;
                IImage image = null;
                if (id == PlaylistService.DynamicFavoritePlaylistId)
                {
                    image = new Library.Imaging.Image(id)
                    {
                        CreatedDate = DateTime.UtcNow
                    };
                    playlistImageFilename = Path.Combine(Configuration.ContentPath, @"images/d.favorite.playlist.jpg");
                }
                else
                {
                    var playlist = await GetPlaylist(id).ConfigureAwait(false);
                    if (playlist == null)
                    {
                        return new FileOperationResult<IImage>(true, $"Playlist Not Found [{id}]");
                    }
                    image = new Library.Imaging.Image(id)
                    {
                        CreatedDate = playlist.CreatedDate
                    };
                    playlistImageFilename = playlist.PathToImage(Configuration);
                }
                try
                {
                    if (File.Exists(playlistImageFilename))
                    {
                        image.Bytes = File.ReadAllBytes(playlistImageFilename);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Image File [{playlistImageFilename}]");
                }
                if (image.Bytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.Playlist;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Playlist Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        /// <summary>
        /// Get image for Release from Release folder, if not exists then use Release.Thumbnail if that is not set then use Release DefaultNotFound image.
        /// </summary>
        private async Task<FileOperationResult<IImage>> ReleaseImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = await GetRelease(id).ConfigureAwait(false);
                if (release == null)
                {
                    return new FileOperationResult<IImage>(true, $"Release Not Found [{id}]");
                }
                byte[] imageBytes = null;
                string artistFolder = null;
                string releaseFolder = null;
                try
                {
                    // See if cover art file exists in release folder
                    artistFolder = release.Artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Logger.LogWarning($"Artist Folder [{artistFolder}], Not Found For Artist `{release.Artist}`");
                    }
                    else
                    {
                        releaseFolder = release.ReleaseFileFolder(artistFolder);
                        if (!Directory.Exists(releaseFolder))
                        {
                            Logger.LogWarning($"Release Folder [{releaseFolder}], Not Found For Release `{release}`");
                        }
                        else
                        {
                            var releaseCoverFiles = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(releaseFolder), ImageType.Release);
                            if (releaseCoverFiles.Any())
                            {
                                imageBytes = File.ReadAllBytes(releaseCoverFiles.First().FullName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Release Folder [{releaseFolder}] Artist Folder [{artistFolder}] For Artist `{release.Artist.Id}`");
                }
                IImage image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
                    CreatedDate = release.CreatedDate
                };
                if (imageBytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.Release;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<IImage>> ReleaseSecondaryImageAction(Guid id, int imageId, EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = await GetRelease(id).ConfigureAwait(false);
                if (release == null)
                {
                    return new FileOperationResult<IImage>(true, $"Release Not Found [{id}]");
                }
                byte[] imageBytes = null;
                string artistFolder = null;
                string releaseFolder = null;
                try
                {
                    // See if cover art file exists in release folder
                    artistFolder = release.Artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Logger.LogWarning($"Artist Folder [{artistFolder}], Not Found For Artist `{release.Artist}`");
                    }
                    else
                    {
                        releaseFolder = release.ReleaseFileFolder(artistFolder);
                        if (!Directory.Exists(releaseFolder))
                        {
                            Logger.LogWarning($"Release Folder [{releaseFolder}], Not Found For Release `{release}`");
                        }
                        else
                        {
                            var releaseSecondaryImages = ImageHelper
                                .FindImageTypeInDirectory(new DirectoryInfo(releaseFolder), ImageType.ReleaseSecondary)
                                .ToArray();
                            if (releaseSecondaryImages.Length >= imageId && releaseSecondaryImages[imageId] != null)
                            {
                                imageBytes = File.ReadAllBytes(releaseSecondaryImages[imageId].FullName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        $"Error Reading Release Folder [{releaseFolder}] Artist Folder [{artistFolder}] For Artist `{release.Artist.Id}`");
                }

                IImage image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
                    CreatedDate = release.CreatedDate
                };
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<IImage>> TrackImageAction(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            try
            {
                var track = await GetTrack(id).ConfigureAwait(false);
                if (track == null)
                {
                    return new FileOperationResult<IImage>(true, $"Track Not Found [{id}]");
                }
                IImage image = new Library.Imaging.Image(id)
                {
                    CreatedDate = track.CreatedDate
                };
                var trackImageFileName = track.PathToTrackThumbnail(Configuration);
                if (File.Exists(trackImageFileName))
                {
                    image.Bytes = File.ReadAllBytes(trackImageFileName);
                }
                if (image.Bytes?.Any() != true)
                {
                    // If no track image is found then return image for release
                    return await ReleaseImageAsync(track.ReleaseMedia.Release.RoadieId, width, height, etag).ConfigureAwait(false);
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Track Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<IImage>> UserImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var user = await GetUser(id).ConfigureAwait(false);
                if (user == null)
                {
                    return new FileOperationResult<IImage>(true, $"User Not Found [{id}]");
                }
                IImage image = new Library.Imaging.Image(id)
                {
                    CreatedDate = user.CreatedDate.Value
                };
                var userImageFilename = user.PathToImage(Configuration);
                try
                {
                    if (File.Exists(userImageFilename))
                    {
                        image.Bytes = File.ReadAllBytes(userImageFilename);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Image File [{userImageFilename}]");
                }
                if (image.Bytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.User;
                }
                return GenerateFileOperationResult(id, image, etag, "image/png");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching User Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        public Task<FileOperationResult<IImage>> ArtistImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation(nameof(ArtistImageAsync),
                data.Artist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ArtistImageAction(id, etag).ConfigureAwait(false); },
                etag);
        }

        public Task<FileOperationResult<IImage>> ArtistSecondaryImageAsync(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation($"ArtistSecondaryThumbnail.{imageId}",
                data.Artist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ArtistSecondaryImageAction(id, imageId, etag).ConfigureAwait(false); },
                etag);
        }

        public Task<FileOperationResult<IImage>> CollectionImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation("CollectionThumbnail",
                data.Collection.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await CollectionImageAction(id, etag).ConfigureAwait(false); },
                etag);
        }

        public Task<FileOperationResult<IImage>> GenreImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation("GenreThumbnail",
                data.Genre.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await GenreImageAction(id, etag).ConfigureAwait(false),
                etag);
        }

        public Task<FileOperationResult<IImage>> LabelImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation("LabelThumbnail",
                data.Label.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await LabelImageAction(id, etag).ConfigureAwait(false),
                etag);
        }

        public Task<FileOperationResult<IImage>> PlaylistImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation("PlaylistThumbnail",
                data.Playlist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await PlaylistImageAction(id, etag).ConfigureAwait(false),
                etag);
        }

        public Task<FileOperationResult<IImage>> ReleaseImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation("ReleaseThumbnail",
                data.Release.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await ReleaseImageAction(id, etag).ConfigureAwait(false),
                etag);
        }

        public Task<FileOperationResult<IImage>> ReleaseSecondaryImageAsync(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation($"ReleaseSecondaryThumbnail-{imageId}",
                data.Release.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ReleaseSecondaryImageAction(id, imageId, etag).ConfigureAwait(false); },
                etag);
        }

        public async Task<OperationResult<IEnumerable<ImageSearchResult>>> SearchAsync(string query, int resultsCount = 10)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            IEnumerable<ImageSearchResult> searchResults = null;
            try
            {
                searchResults = await ImageSearchManager.ImageSearch(query).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();
            return new OperationResult<IEnumerable<ImageSearchResult>>
            {
                Data = searchResults,
                IsSuccess = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public Task<FileOperationResult<IImage>> TrackImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation("TrackThumbnail",
                data.Track.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await TrackImageAction(id, width, height, etag).ConfigureAwait(false),
                etag);
        }

        public Task<FileOperationResult<IImage>> UserImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return GetImageFileOperation("UserById",
                User.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await UserImageAction(id, etag).ConfigureAwait(false),
                etag);
        }
    }
}