using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
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
        public string Referrer { get; set; }
        public string RequestIp { get; set; }
        private IDefaultNotFoundImages DefaultNotFoundImages { get; }

        private IImageSearchManager ImageSearchManager { get; }

        public ImageService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            data.IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<ImageService> logger,
            IDefaultNotFoundImages defaultNotFoundImages,
            IImageSearchManager imageSearchManager)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            DefaultNotFoundImages = defaultNotFoundImages;
            ImageSearchManager = imageSearchManager;
        }

        public async Task<FileOperationResult<Image>> ArtistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("ArtistImage",
                data.Artist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ArtistImageAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> ArtistSecondaryImage(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation($"ArtistSecondaryThumbnail-{imageId}",
                data.Release.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ArtistSecondaryImageAction(id, imageId, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> ById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("ImageById",
                data.Image.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ImageByIdAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> CollectionImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("CollectionThumbnail",
                data.Collection.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await CollectionImageAction(id, etag); },
                etag);
        }

        public async Task<OperationResult<bool>> Delete(ApplicationUser user, Guid id)
        {
            var sw = Stopwatch.StartNew();
            var image = DbContext.Images
                .Include("Release")
                .Include("Artist")
                .FirstOrDefault(x => x.RoadieId == id);
            if (image == null) return new OperationResult<bool>(true, string.Format("Image Not Found [{0}]", id));
            DbContext.Images.Remove(image);
            await DbContext.SaveChangesAsync();
            if (image.ArtistId.HasValue) CacheManager.ClearRegion(data.Artist.CacheRegionUrn(image.Artist.RoadieId));
            if (image.ReleaseId.HasValue) CacheManager.ClearRegion(data.Release.CacheRegionUrn(image.Release.RoadieId));
            CacheManager.ClearRegion(data.Image.CacheRegionUrn(id));
            Logger.LogWarning("User `{0}` deleted Image `{1}]`", user, image);
            sw.Stop();
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<FileOperationResult<Image>> GenreImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("GenreThumbnail",
                data.Label.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await GenreImageAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> LabelImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("LabelThumbnail",
                data.Label.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await LabelImageAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> PlaylistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("PlaylistThumbnail",
                data.Playlist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await PlaylistImageAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> ReleaseImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("ReleaseThumbnail",
                data.Release.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ReleaseImageAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> ReleaseSecondaryImage(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation($"ReleaseSecondaryThumbnail-{imageId}",
                data.Release.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ReleaseSecondaryImageAction(id, imageId, etag); },
                etag);
        }

        public async Task<OperationResult<IEnumerable<ImageSearchResult>>> Search(string query, int resultsCount = 10)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            IEnumerable<ImageSearchResult> searchResults = null;
            try
            {
                searchResults = await ImageSearchManager.ImageSearch(query);
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
                IsSuccess = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<FileOperationResult<Image>> TrackImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("TrackThumbnail",
                data.Track.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await TrackImageAction(id, width, height, etag); },
                etag);
        }

        public async Task<FileOperationResult<Image>> UserImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("UserById",
                ApplicationUser.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await UserImageAction(id, etag); },
                etag);
        }

        /// <summary>
        /// Get image for an artist, see if the artist has an image in their folder and use that else use Artist.Thumbnail, is also not found use Artist DefaultNotFound image.
        /// </summary>
        private Task<FileOperationResult<Image>> ArtistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = GetArtist(id);
                if (artist == null)
                {
                    return Task.FromResult(new FileOperationResult<Image>(true, string.Format("Artist Not Found [{0}]", id)));
                }
                byte[] imageBytes = null;
                string artistFolder = null;
                try
                {
                    // See if artist images exists in artist folder
                    artistFolder = artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Logger.LogTrace($"Artist Folder [{artistFolder}], Not Found For Artist `{artist}`");
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

                imageBytes = imageBytes ?? artist.Thumbnail;
                var image = new data.Image
                {
                    Bytes = imageBytes,
                    CreatedDate = artist.CreatedDate,
                    LastUpdated = artist.LastUpdated
                };
                if (imageBytes == null || !imageBytes.Any())
                {
                    image = DefaultNotFoundImages.Artist;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Artist Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<Image>> ArtistSecondaryImageAction(Guid id, int imageId, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = GetArtist(id);
                if (artist == null)
                    return Task.FromResult(new FileOperationResult<Image>(true,
                        string.Format("Release Not Found [{0}]", id)));
                byte[] imageBytes = null;
                string artistFolder = null;
                try
                {
                    // See if cover art file exists in release folder
                    artistFolder = artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Logger.LogTrace($"Artist Folder [{artistFolder}], Not Found For Artist `{artist}`");
                    }
                    else
                    {
                        var artistSecondaryImages = ImageHelper
                            .FindImageTypeInDirectory(new DirectoryInfo(artistFolder), ImageType.ArtistSecondary)
                            .ToArray();
                        if (artistSecondaryImages.Length >= imageId && artistSecondaryImages[imageId] != null)
                            imageBytes = File.ReadAllBytes(artistSecondaryImages[imageId].FullName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error Reading Artist Folder [{artistFolder}] For Artist `{artist}`");
                }

                var image = new data.Image
                {
                    Bytes = imageBytes,
                    CreatedDate = artist.CreatedDate,
                    LastUpdated = artist.LastUpdated
                };
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<Image>> CollectionImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var collection = GetCollection(id);
                if (collection == null)
                {
                    return Task.FromResult(new FileOperationResult<Image>(true, string.Format("Collection Not Found [{0}]", id)));
                }
                var image = new data.Image
                {
                    Bytes = collection.Thumbnail,
                    CreatedDate = collection.CreatedDate,
                    LastUpdated = collection.LastUpdated
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
                if (collection.Thumbnail == null || !collection.Thumbnail.Any())
                {
                    image = DefaultNotFoundImages.Collection;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Collection Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private FileOperationResult<Image> GenerateFileOperationResult(Guid id, data.Image image,
            EntityTagHeaderValue etag = null, string contentType = "image/jpeg")
        {
            var imageEtag = EtagHelper.GenerateETag(HttpEncoder, image.Bytes);
            if (EtagHelper.CompareETag(HttpEncoder, etag, imageEtag))
                return new FileOperationResult<Image>(OperationMessages.NotModified);
            if (!image?.Bytes?.Any() ?? false)
                return new FileOperationResult<Image>(string.Format("ImageById Not Set [{0}]", id));
            return new FileOperationResult<Image>(image?.Bytes?.Any() ?? false
                ? OperationMessages.OkMessage
                : OperationMessages.NoImageDataFound)
            {
                IsSuccess = true,
                Data = image.Adapt<Image>(),
                ContentType = contentType,
                LastModified = image.LastUpdated ?? image.CreatedDate,
                ETag = imageEtag
            };
        }

        private Task<FileOperationResult<Image>> GenreImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var genre = GetGenre(id);
                if (genre == null)
                {
                    return Task.FromResult(new FileOperationResult<Image>(true, string.Format("Genre Not Found [{0}]", id)));
                }
                var image = new data.Image
                {
                    Bytes = genre.Thumbnail,
                    CreatedDate = genre.CreatedDate,
                    LastUpdated = genre.LastUpdated
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
                if (genre.Thumbnail == null || !genre.Thumbnail.Any())
                {
                    image = DefaultNotFoundImages.Genre;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Label Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private async Task<FileOperationResult<Image>> GetImageFileOperation(string type, string regionUrn, Guid id, int? width, int? height, Func<Task<FileOperationResult<Image>>> action, EntityTagHeaderValue etag = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var result = (await CacheManager.GetAsync($"urn:{type}_by_id_operation:{id}", action, regionUrn)).Adapt<FileOperationResult<Image>>();
                if (!result.IsSuccess) return new FileOperationResult<Image>(result.IsNotFoundResult, result.Messages);
                if (result.ETag == etag) return new FileOperationResult<Image>(OperationMessages.NotModified);
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
                return new FileOperationResult<Image>(result.Messages)
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

            return new FileOperationResult<Image>("System Error");
        }

        private Task<FileOperationResult<Image>> ImageByIdAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var image = DbContext.Images
                    .Include("Release")
                    .Include("Artist")
                    .FirstOrDefault(x => x.RoadieId == id);
                if (image == null)
                    return Task.FromResult(new FileOperationResult<Image>(true,
                        string.Format("ImageById Not Found [{0}]", id)));
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Image [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<Image>> LabelImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var label = GetLabel(id);
                if (label == null)
                    return Task.FromResult(new FileOperationResult<Image>(true,
                        string.Format("Label Not Found [{0}]", id)));
                var image = new data.Image
                {
                    Bytes = label.Thumbnail,
                    CreatedDate = label.CreatedDate,
                    LastUpdated = label.LastUpdated
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
                if (label.Thumbnail == null || !label.Thumbnail.Any())
                {
                    image = DefaultNotFoundImages.Label;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Label Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<Image>> PlaylistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var playlist = GetPlaylist(id);
                if (playlist == null)
                    return Task.FromResult(new FileOperationResult<Image>(true,
                        string.Format("Playlist Not Found [{0}]", id)));
                var image = new data.Image
                {
                    Bytes = playlist.Thumbnail,
                    CreatedDate = playlist.CreatedDate,
                    LastUpdated = playlist.LastUpdated
                };
                var playlistImageFilename = playlist.PathToImage(Configuration);
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
                if (playlist.Thumbnail == null || !playlist.Thumbnail.Any())
                {
                    image = DefaultNotFoundImages.Playlist;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Playlist Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        /// <summary>
        /// Get image for Release from Release folder, if not exists then use Release.Thumbnail if that is not set then use Release DefaultNotFound image.
        /// </summary>
        private Task<FileOperationResult<Image>> ReleaseImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = GetRelease(id);
                if (release == null)
                {
                    return Task.FromResult(new FileOperationResult<Image>(true, string.Format("Release Not Found [{0}]", id)));
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
                        Logger.LogTrace($"Artist Folder [{artistFolder}], Not Found For Artist `{release.Artist}`");
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

                imageBytes = imageBytes ?? release.Thumbnail;
                var image = new data.Image
                {
                    Bytes = imageBytes,
                    CreatedDate = release.CreatedDate,
                    LastUpdated = release.LastUpdated
                };
                if (release.Thumbnail == null || !release.Thumbnail.Any())
                {
                    image = DefaultNotFoundImages.Release;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<Image>> ReleaseSecondaryImageAction(Guid id, int imageId,
            EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = GetRelease(id);
                if (release == null)
                    return Task.FromResult(new FileOperationResult<Image>(true,
                        string.Format("Release Not Found [{0}]", id)));
                byte[] imageBytes = null;
                string artistFolder = null;
                string releaseFolder = null;
                try
                {
                    // See if cover art file exists in release folder
                    artistFolder = release.Artist.ArtistFileFolder(Configuration);
                    if (!Directory.Exists(artistFolder))
                    {
                        Logger.LogTrace($"Artist Folder [{artistFolder}], Not Found For Artist `{release.Artist}`");
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
                                imageBytes = File.ReadAllBytes(releaseSecondaryImages[imageId].FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        $"Error Reading Release Folder [{releaseFolder}] Artist Folder [{artistFolder}] For Artist `{release.Artist.Id}`");
                }

                var image = new data.Image
                {
                    Bytes = imageBytes,
                    CreatedDate = release.CreatedDate,
                    LastUpdated = release.LastUpdated
                };
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }

        private async Task<FileOperationResult<Image>> TrackImageAction(Guid id, int? width, int? height,
            EntityTagHeaderValue etag = null)
        {
            try
            {
                var track = GetTrack(id);
                if (track == null)
                    return new FileOperationResult<Image>(true, string.Format("Track Not Found [{0}]", id));
                var imageBytes = track.Thumbnail;
                var trackThumbnailImages = ImageHelper.FindImageTypeInDirectory(
                    new DirectoryInfo(track.PathToTrackThumbnail(Configuration)),
                    ImageType.Track, SearchOption.TopDirectoryOnly);
                if (trackThumbnailImages.Any()) imageBytes = File.ReadAllBytes(trackThumbnailImages.First().FullName);
                var image = new data.Image
                {
                    Bytes = track.Thumbnail,
                    CreatedDate = track.CreatedDate,
                    LastUpdated = track.LastUpdated
                };
                if (track.Thumbnail == null || !track.Thumbnail.Any())
                    // If no track image is found then return image for release
                    return await ReleaseImage(track.ReleaseMedia.Release.RoadieId, width, height, etag);
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Track Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private Task<FileOperationResult<Image>> UserImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var user = GetUser(id);
                if (user == null)
                {
                    return Task.FromResult(new FileOperationResult<Image>(true, string.Format("User Not Found [{0}]", id)));
                }
                var image = new data.Image
                {
                    Bytes = user.Avatar,
                    CreatedDate = user.CreatedDate.Value,
                    LastUpdated = user.LastUpdated
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
                if (!(image?.Bytes?.Any() ?? false))
                {
                    image = DefaultNotFoundImages.User;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag, "image/png"));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching User Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<Image>(OperationMessages.ErrorOccured));
        }
    }
}