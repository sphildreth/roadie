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

        public ImageService(IRoadieSettings configuration, data.IRoadieDbContext dbContext, ICacheManager cacheManager, 
                            ILogger logger, DefaultNotFoundImages defaultNotFoundImages)
            : base(configuration, null, dbContext, cacheManager, logger, null)
        {
            DefaultNotFoundImages = defaultNotFoundImages;
        }

        public async Task<FileOperationResult<IImage>> ArtistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("ArtistImage",
                data.Artist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ArtistImageAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<IImage>> ArtistSecondaryImage(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation($"ArtistSecondaryThumbnail.{imageId}",
                data.Artist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ArtistSecondaryImageAction(id, imageId, etag); },
                etag);
        }

        public async Task<FileOperationResult<IImage>> ById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("ImageById",
                Library.Imaging.Image.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await ImageByIdAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<IImage>> CollectionImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("CollectionThumbnail",
                data.Collection.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => { return await CollectionImageAction(id, etag); },
                etag);
        }

        public async Task<FileOperationResult<IImage>> GenreImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("GenreThumbnail",
                data.Genre.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await GenreImageAction(id, etag),
                etag);
        }

        public async Task<FileOperationResult<IImage>> LabelImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("LabelThumbnail",
                data.Label.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await LabelImageAction(id, etag),
                etag);
        }

        public async Task<FileOperationResult<IImage>> PlaylistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("PlaylistThumbnail",
                data.Playlist.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await PlaylistImageAction(id, etag),
                etag);
        }

        public async Task<FileOperationResult<IImage>> ReleaseImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("ReleaseThumbnail",
                data.Release.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await ReleaseImageAction(id, etag),
                etag);
        }

        public async Task<FileOperationResult<IImage>> ReleaseSecondaryImage(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null)
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

        public async Task<FileOperationResult<IImage>> TrackImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("TrackThumbnail",
                data.Track.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await TrackImageAction(id, width, height, etag),
                etag);
        }

        public async Task<FileOperationResult<IImage>> UserImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await GetImageFileOperation("UserById",
                ApplicationUser.CacheRegionUrn(id),
                id,
                width,
                height,
                async () => await UserImageAction(id, etag),
                etag);
        }

        /// <summary>
        /// Get image for an artist, see if the artist has an image in their folder and use that else use Artist.Thumbnail, is also not found use Artist DefaultNotFound image.
        /// </summary>
        private Task<FileOperationResult<IImage>> ArtistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = GetArtist(id);
                if (artist == null)
                {
                    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("Artist Not Found [{0}]", id)));
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

                IImage image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
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

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<IImage>> ArtistSecondaryImageAction(Guid id, int imageId, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = GetArtist(id);
                if (artist == null)
                {
                    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("Release Not Found [{0}]", id)));
                }
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

                var image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
                    CreatedDate = artist.CreatedDate
                };
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<IImage>> CollectionImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var collection = GetCollection(id);
                if (collection == null)
                {
                    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("Collection Not Found [{0}]", id)));
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
                if (image.Bytes == null || !image.Bytes.Any())
                {
                    image = DefaultNotFoundImages.Collection;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Collection Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
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
                return new FileOperationResult<IImage>(string.Format("ImageById Not Set [{0}]", id));
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

        private Task<FileOperationResult<IImage>> GenreImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var genre = GetGenre(id);
                if (genre == null)
                {
                    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("Genre Not Found [{0}]", id)));
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
                if (image.Bytes == null || !image.Bytes.Any())
                {
                    image = DefaultNotFoundImages.Genre;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Label Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        private async Task<FileOperationResult<IImage>> GetImageFileOperation(string type, string regionUrn, Guid id, int? width, int? height, Func<Task<FileOperationResult<IImage>>> action, EntityTagHeaderValue etag = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var sizeHash = (width ?? 0) + (height ?? 0);
                var result = await CacheManager.GetAsync($"urn:{type}_by_id_operation:{id}:{sizeHash}", action, regionUrn);
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

        private Task<FileOperationResult<IImage>> ImageByIdAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                // TODO #29; fetch image by ID

                throw new NotImplementedException();

                //var image = DbContext.Images
                //    .Include("Release")
                //    .Include("Artist")
                //    .FirstOrDefault(x => x.RoadieId == id);
                //if (image == null)
                //{
                //    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("ImageById Not Found [{0}]", id)));
                //}
                //return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Image [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<IImage>> LabelImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var label = GetLabel(id);
                if (label == null)
                    return Task.FromResult(new FileOperationResult<IImage>(true,
                        string.Format("Label Not Found [{0}]", id)));
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
                if (image.Bytes == null || !image.Bytes.Any())
                {
                    image = DefaultNotFoundImages.Label;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Label Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<IImage>> PlaylistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var playlist = GetPlaylist(id);
                if (playlist == null)
                    return Task.FromResult(new FileOperationResult<IImage>(true,
                        string.Format("Playlist Not Found [{0}]", id)));
                IImage image = new Library.Imaging.Image(id)
                {
                    CreatedDate = playlist.CreatedDate
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
                if (image.Bytes == null || !image.Bytes.Any())
                {
                    image = DefaultNotFoundImages.Playlist;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Playlist Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        /// <summary>
        /// Get image for Release from Release folder, if not exists then use Release.Thumbnail if that is not set then use Release DefaultNotFound image.
        /// </summary>
        private Task<FileOperationResult<IImage>> ReleaseImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = GetRelease(id);
                if (release == null)
                {
                    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("Release Not Found [{0}]", id)));
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
                IImage image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
                    CreatedDate = release.CreatedDate
                };
                if (imageBytes?.Any() != true)
                {
                    image = DefaultNotFoundImages.Release;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        private Task<FileOperationResult<IImage>> ReleaseSecondaryImageAction(Guid id, int imageId,
            EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = GetRelease(id);
                if (release == null)
                {
                    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("Release Not Found [{0}]", id)));
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

                IImage image = new Library.Imaging.Image(id)
                {
                    Bytes = imageBytes,
                    CreatedDate = release.CreatedDate
                };
                return Task.FromResult(GenerateFileOperationResult(id, image, etag));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Release Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }

        private async Task<FileOperationResult<IImage>> TrackImageAction(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            try
            {
                var track = GetTrack(id);
                if (track == null)
                {
                    return new FileOperationResult<IImage>(true, string.Format("Track Not Found [{0}]", id));
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
                if (image.Bytes == null || !image.Bytes.Any())
                {
                    // If no track image is found then return image for release
                    return await ReleaseImage(track.ReleaseMedia.Release.RoadieId, width, height, etag);
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching Track Thumbnail [{id}]", ex);
            }

            return new FileOperationResult<IImage>(OperationMessages.ErrorOccured);
        }

        private Task<FileOperationResult<IImage>> UserImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var user = GetUser(id);
                if (user == null)
                {
                    return Task.FromResult(new FileOperationResult<IImage>(true, string.Format("User Not Found [{0}]", id)));
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
                if (image.Bytes == null || !image.Bytes.Any())
                {
                    image = DefaultNotFoundImages.User;
                }
                return Task.FromResult(GenerateFileOperationResult(id, image, etag, "image/png"));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching User Thumbnail [{id}]", ex);
            }

            return Task.FromResult(new FileOperationResult<IImage>(OperationMessages.ErrorOccured));
        }
    }
}