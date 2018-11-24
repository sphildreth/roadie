using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.Models.Users;
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

        public ImageService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ImageService> logger,
                             IDefaultNotFoundImages defaultNotFoundImages)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            this.DefaultNotFoundImages = defaultNotFoundImages;
        }

        public async Task<FileOperationResult<Image>> ArtistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "ArtistImage",
                                                    regionUrn: data.Artist.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.ArtistImageAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> ById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "ImageById",
                                                    regionUrn: data.Image.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.ImageByIdAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> CollectionImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "CollectionThumbnail",
                                                    regionUrn: data.Collection.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.CollectionImageAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<OperationResult<bool>> Delete(User user, Guid id)
        {
            var sw = Stopwatch.StartNew();
            var image = this.DbContext.Images
                                      .Include("Release")
                                      .Include("Artist")
                                      .FirstOrDefault(x => x.RoadieId == id);
            if (image == null)
            {
                return new OperationResult<bool>(true, string.Format("Image Not Found [{0}]", id));
            }
            if (image.ArtistId.HasValue)
            {
                this.CacheManager.ClearRegion(data.Artist.CacheRegionUrn(image.Artist.RoadieId));
            }
            if (image.ReleaseId.HasValue)
            {
                this.CacheManager.ClearRegion(data.Release.CacheRegionUrn(image.Release.RoadieId));
            }
            this.DbContext.Images.Remove(image);
            await this.DbContext.SaveChangesAsync();
            this.CacheManager.ClearRegion(data.Image.CacheRegionUrn(id));
            this.Logger.LogInformation($"Deleted Image [{ id }], By User [{ user.ToString() }]");
            sw.Stop();
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<IEnumerable<ImageSearchResult>>> ImageProvidersSearch(string query)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            IEnumerable<ImageSearchResult> searchResults = null;
            try
            {
                var manager = new ImageSearchManager(this.Configuration, this.CacheManager, this.Logger);
                searchResults = await manager.ImageSearch(query);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
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

        public async Task<FileOperationResult<Image>> LabelImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "LabelThumbnail",
                                                    regionUrn: data.Label.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.LabelImageAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> PlaylistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "PlaylistThumbnail",
                                                    regionUrn: data.Playlist.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.PlaylistImageAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> ReleaseImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "ReleaseThumbnail",
                                                    regionUrn: data.Release.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.ReleaseImageAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> TrackImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "TrackThumbnail",
                                                    regionUrn: data.Track.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.TrackImageAction(id, width, height, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> UserImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "UserById",
                                                    regionUrn: ApplicationUser.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.UserImageAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        private async Task<FileOperationResult<Image>> ArtistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = this.GetArtist(id);
                if (artist == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Artist Not Found [{0}]", id));
                }
                byte[] imageBytes = null;
                string artistFolder = null;
                try
                {
                    // See if artist images exists in artist folder
                    artistFolder = artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                    if (!Directory.Exists(artistFolder))
                    {
                        this.Logger.LogWarning($"Artist Folder [{ artistFolder }], Not Found For Artist [{ artist.ToString() }]");
                    }
                    else
                    {
                        var artistImages = Directory.GetFiles(artistFolder, "artist*.*");
                        if (artistImages.Any())
                        {
                            imageBytes = File.ReadAllBytes(artistImages.First());
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, $"Error Reading Folder [{ artistFolder }] For Artist [{ artist.Id }]");
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
                    image = this.DefaultNotFoundImages.Artist;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Artist Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> CollectionImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var collection = this.GetCollection(id);
                if (collection == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Collection Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = collection.Thumbnail,
                    CreatedDate = collection.CreatedDate,
                    LastUpdated = collection.LastUpdated
                };
                if (collection.Thumbnail == null || !collection.Thumbnail.Any())
                {
                    image = this.DefaultNotFoundImages.Collection;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Collection Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private FileOperationResult<Image> GenerateFileOperationResult(Guid id, data.Image image, EntityTagHeaderValue etag = null, string contentType = "image/jpeg")
        {
            var imageEtag = EtagHelper.GenerateETag(this.HttpEncoder, image.Bytes);
            if (EtagHelper.CompareETag(this.HttpEncoder, etag, imageEtag))
            {
                return new FileOperationResult<Image>(OperationMessages.NotModified);
            }
            if (!image?.Bytes?.Any() ?? false)
            {
                return new FileOperationResult<Image>(string.Format("ImageById Not Set [{0}]", id));
            }
            return new FileOperationResult<Image>(image?.Bytes?.Any() ?? false ? OperationMessages.OkMessage : OperationMessages.NoImageDataFound)
            {
                IsSuccess = true,
                Data = image.Adapt<Image>(),
                ContentType = contentType,
                LastModified = (image.LastUpdated ?? image.CreatedDate),
                ETag = imageEtag
            };
        }

        private async Task<FileOperationResult<Image>> GetImageFileOperation(string type, string regionUrn, Guid id, int? width, int? height, Func<Task<FileOperationResult<Image>>> action, EntityTagHeaderValue etag = null)
        {
            var sw = Stopwatch.StartNew();
            var result = (await this.CacheManager.GetAsync($"urn:{ type }_by_id_operation:{id}", action, regionUrn)).Adapt<FileOperationResult<Image>>();
            if (!result.IsSuccess)
            {
                return new FileOperationResult<Image>(result.IsNotFoundResult, result.Messages);
            }
            if (result.ETag == etag)
            {
                return new FileOperationResult<Image>(OperationMessages.NotModified);
            }
            if ((width.HasValue || height.HasValue) && result?.Data?.Bytes != null)
            {
                result.Data.Bytes = ImageHelper.ResizeImage(result?.Data?.Bytes, width.Value, height.Value);
                result.ETag = EtagHelper.GenerateETag(this.HttpEncoder, result.Data.Bytes);
                result.LastModified = DateTime.UtcNow;
                if (width.Value != this.Configuration.ThumbnailImageSize.Width || height.Value != this.Configuration.ThumbnailImageSize.Height)
                {
                    this.Logger.LogInformation($"{ type }: Resized [{ id }], Width [{ width.Value }], Height [{ height.Value }]");
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

        private async Task<FileOperationResult<Image>> ImageByIdAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var image = this.DbContext.Images
                                          .Include("Release")
                                          .Include("Artist")
                                          .FirstOrDefault(x => x.RoadieId == id);
                if (image == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("ImageById Not Found [{0}]", id));
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Image [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> LabelImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var label = this.GetLabel(id);
                if (label == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Label Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = label.Thumbnail,
                    CreatedDate = label.CreatedDate,
                    LastUpdated = label.LastUpdated
                };
                if (label.Thumbnail == null || !label.Thumbnail.Any())
                {
                    image = this.DefaultNotFoundImages.Label;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Label Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> PlaylistImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var playlist = this.GetPlaylist(id);
                if (playlist == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Playlist Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = playlist.Thumbnail,
                    CreatedDate = playlist.CreatedDate,
                    LastUpdated = playlist.LastUpdated
                };
                if (playlist.Thumbnail == null || !playlist.Thumbnail.Any())
                {
                    image = this.DefaultNotFoundImages.Playlist;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Playlist Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> ReleaseImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = this.GetRelease(id);
                if (release == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Release Not Found [{0}]", id));
                }
                byte[] imageBytes = null;
                string artistFolder = null;
                string releaseFolder = null;
                try
                {
                    // See if cover art file exists in release folder
                    artistFolder = release.Artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                    if (!Directory.Exists(artistFolder))
                    {
                        this.Logger.LogWarning($"Artist Folder [{ artistFolder }], Not Found For Artist [{ release.Artist.ToString() }]");
                    }
                    else
                    {
                        releaseFolder = release.ReleaseFileFolder(artistFolder);
                        if (!Directory.Exists(releaseFolder))
                        {
                            this.Logger.LogWarning($"Release Folder [{ releaseFolder }], Not Found For Release [{ release.ToString() }]");
                        }
                        else
                        {
                            var coverArtFiles = Directory.GetFiles(releaseFolder, "cover*.*");
                            if (coverArtFiles.Any())
                            {
                                imageBytes = File.ReadAllBytes(coverArtFiles.First());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, $"Error Reading Release Folder [{ releaseFolder }] Artist Folder [{ artistFolder }] For Artist [{ release.Artist.Id }]");
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
                    image = this.DefaultNotFoundImages.Release;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Release Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> TrackImageAction(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            try
            {
                var track = this.GetTrack(id);
                if (track == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Track Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = track.Thumbnail,
                    CreatedDate = track.CreatedDate,
                    LastUpdated = track.LastUpdated
                };
                if (track.Thumbnail == null || !track.Thumbnail.Any())
                {
                    // If no track image is found then return image for release
                    return await this.ReleaseImage(track.ReleaseMedia.Release.RoadieId, width, height, etag);
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Track Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> UserImageAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var user = this.GetUser(id);
                if (user == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("User Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = user.Avatar,
                    CreatedDate = user.CreatedDate.Value,
                    LastUpdated = user.LastUpdated
                };
                if (user.Avatar == null || !user.Avatar.Any())
                {
                    image = this.DefaultNotFoundImages.User;
                }
                return GenerateFileOperationResult(id, image, etag, "image/png");
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching User Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }
    }
}