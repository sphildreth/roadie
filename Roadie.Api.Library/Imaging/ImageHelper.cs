using ImageMagick;
using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using models = Roadie.Library.Models;

namespace Roadie.Library.Imaging
{
    public static class ImageHelper
    {
        public static string ArtistImageFilename = "artist.jpg";

        public static string ArtistSecondaryImageFilename = "artist {0}.jpg"; // Replace with counter of image

        public static string ReleaseCoverFilename = "cover.jpg";

        public static string ReleaseSecondaryImageFilename = "release {0}.jpg"; // Replace with counter of image

        /// <summary>
        /// Only user avatars are GIF to allow for animation.
        /// </summary>
        public static byte[] ConvertToGifFormat(byte[] imageBytes)
        {
            if (imageBytes?.Any() != true)
            {
                return null;
            }
            try
            {
                using (var outStream = new MemoryStream())
                {
                    IImageFormat imageFormat = null;
                    using (var image = SixLabors.ImageSharp.Image.Load(imageBytes, out imageFormat))
                    {
                        image.SaveAsGif(outStream);
                    }
                    return outStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Converting Image to Gif [{ ex.Message }]", "Warning");
            }
            return null;
        }

        public static byte[] ConvertToJpegFormat(byte[] imageBytes)
        {
            if (imageBytes?.Any() != true)
            {
                return null;
            }

            try
            {
                return ConvertToJpegFormatViaSixLabors(imageBytes);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Converting Image to Jpg via SixLabors [{ ex }]", "Warning");
            }
            try
            {
                return ConvertToJpegFormatViaMagick(imageBytes);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Converting Image to Jpg via Magick [{ ex }]", "Warning");
            }
            return null;
        }

        public static byte[] ConvertToJpegFormatViaSixLabors(byte[] imageBytes)
        {
            using (var outStream = new MemoryStream())
            {
                IImageFormat imageFormat = null;
                using (var image = SixLabors.ImageSharp.Image.Load(imageBytes, out imageFormat))
                {
                    image.SaveAsJpeg(outStream);
                }
                return outStream.ToArray();
            }
        }

        public static byte[] ConvertToJpegFormatViaMagick(byte[] imageBytes)
        {
            using (var image = new MagickImage(imageBytes))
            {
                image.Format = MagickFormat.Jpeg;
                return image.ToByteArray();
            }
        }

        public static IEnumerable<FileInfo> FindImagesByName(DirectoryInfo directory,
                                                             string name,
                                                             SearchOption folderSearchOptions = SearchOption.AllDirectories)
        {
            var result = new List<FileInfo>();
            if (directory?.Exists != true || string.IsNullOrEmpty(name))
            {
                return result;
            }

            var imageFilesInFolder = ImageFilesInFolder(directory.FullName, folderSearchOptions);
            if (imageFilesInFolder?.Any() != true)
            {
                return result;
            }

            if (imageFilesInFolder.Length > 0)
            {
                name = name.ToAlphanumericName();
                foreach (var imageFileInFolder in imageFilesInFolder)
                {
                    var image = new FileInfo(imageFileInFolder);
                    var filenameWithoutExtension = Path.GetFileName(imageFileInFolder).ToAlphanumericName();
                    var imageName = image.Name.ToAlphanumericName();
                    if (imageName.Equals(name) || filenameWithoutExtension.Equals(name))
                    {
                        result.Add(image);
                    }
                }
            }

            return result;
        }

        public static IEnumerable<FileInfo> FindImageTypeInDirectory(DirectoryInfo directory,
                                                                     ImageType type,
                                                                     SearchOption folderSearchOptions = SearchOption.AllDirectories)
        {
            var result = new List<FileInfo>();
            if (directory?.Exists != true)
            {
                return result;
            }
            var imageFilesInFolder = ImageFilesInFolder(directory.FullName, folderSearchOptions);
            if (imageFilesInFolder?.Any() != true)
            {
                return result;
            }
            foreach (var imageFile in imageFilesInFolder)
            {
                var image = new FileInfo(imageFile);
                switch (type)
                {
                    case ImageType.Artist:
                        if (IsArtistImage(image))
                        {
                            result.Add(image);
                        }
                        break;

                    case ImageType.ArtistSecondary:
                        if (IsArtistSecondaryImage(image))
                        {
                            result.Add(image);
                        }
                        break;

                    case ImageType.Release:
                        if (IsReleaseImage(image))
                        {
                            result.Add(image);
                        }
                        break;

                    case ImageType.ReleaseSecondary:
                        if (IsReleaseSecondaryImage(image))
                        {
                            result.Add(image);
                        }
                        break;

                    case ImageType.Label:
                        if (IsLabelImage(image))
                        {
                            result.Add(image);
                        }
                        break;
                }
            }
            return result.OrderBy(x => x.Name);
        }

        public static string[] GetFiles(string path,
                                        string[] patterns = null,
                                        SearchOption options = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(path))
            {
                return new string[0];
            }
            if (patterns == null || patterns.Length == 0)
            {
                return Directory.GetFiles(path, "*", options);
            }
            if (patterns.Length == 1)
            {
                return Directory.GetFiles(path, patterns[0], options);
            }
            return patterns.SelectMany(pattern => Directory.GetFiles(path, pattern, options)).Distinct().ToArray();
        }

        /// <summary>
        /// Get image data from all sources for either fileanme or MetaData
        /// </summary>
        /// <param name="filename">Name of the File (ie a CUE file)</param>
        /// <param name="metaData">Populated MetaData</param>
        public static AudioMetaDataImage GetPictureForMetaData(IRoadieSettings configuration,
                                                               string filename,
                                                               AudioMetaData metaData)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(filename), "Invalid Filename");
            SimpleContract.Requires<ArgumentException>(metaData != null, "Invalid MetaData");

            return ImageForFilename(configuration, filename);
        }

        public static byte[] ImageDataFromUrl(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var dataString = imageUrl.Trim()
                    .Replace('-', '+')
                    .Replace("data:image/jpeg;base64,", string.Empty)
                    .Replace("data:image/bmp;base64,", string.Empty)
                    .Replace("data:image/gif;base64,", string.Empty)
                    .Replace("data:image/png;base64,", string.Empty)
                    .Replace("data:image/webp;base64,", string.Empty);
                return Convert.FromBase64String(dataString);
            }

            return null;
        }

        public static string[] ImageExtensions()
        { return new string[7] { "*.bmp", "*.jpeg", "*.jpe", "*.jpg", "*.png", "*.gif", "*.webp" }; }

        public static string[] ImageFilesInFolder(string folder, SearchOption searchOption)
        { return GetFiles(folder, ImageExtensions(), searchOption); }

        /// <summary>
        /// Does image exist with the same filename
        /// </summary>
        /// <param name="filename">Name of the File (ie a CUE file)</param>
        /// <returns>Null if not found else populated image</returns>
        public static AudioMetaDataImage ImageForFilename(IRoadieSettings configuration, string filename)
        {
            AudioMetaDataImage imageMetaData = null;

            if (string.IsNullOrEmpty(filename))
            {
                return imageMetaData;
            }
            try
            {
                var fileInfo = new FileInfo(filename);
                var ReleaseCover = Path.ChangeExtension(filename, "jpg");
                if (File.Exists(ReleaseCover))
                {
                    using (var processor = new ImageProcessor(configuration))
                    {
                        imageMetaData = new AudioMetaDataImage
                        {
                            Data = processor.Process(File.ReadAllBytes(ReleaseCover)),
                            Type = AudioMetaDataImageType.FrontCover,
                            MimeType = Library.Processors.FileProcessor.DetermineFileType(fileInfo)
                        };
                    }
                }
                else
                {
                    // Is there a picture in filename folder (for the Release)
                    var pictures = fileInfo.Directory.GetFiles("*.jpg");
                    var tagImages = new List<AudioMetaDataImage>();
                    if (pictures?.Any() == true)
                    {
                        // See if there is a "cover" or "front" jpg file if so use it
                        FileInfo picture = Array.Find(pictures,
                                                      x => x.Name.Equals("cover", StringComparison.OrdinalIgnoreCase));
                        if (picture == null)
                        {
                            picture = Array.Find(pictures,
                                                x => x.Name.Equals("front", StringComparison.OrdinalIgnoreCase));
                        }

                        if (picture == null)
                        {
                            picture = pictures[0];
                        }

                        if (picture != null)
                        {
                            using (var processor = new ImageProcessor(configuration))
                            {
                                imageMetaData = new AudioMetaDataImage
                                {
                                    Data = processor.Process(File.ReadAllBytes(picture.FullName)),
                                    Type = AudioMetaDataImageType.FrontCover,
                                    MimeType = Library.Processors.FileProcessor.DetermineFileType(picture)
                                };
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex, ex.Serialize());
            }

            return imageMetaData;
        }

        public static string[] ImageMimeTypes()
        { return new string[4] { "image/bmp", "image/jpeg", "image/png", "image/gif" }; }

        public static ImageSearchResult ImageSearchResultForImageUrl(string imageUrl)
        {
            if (!WebHelper.IsStringUrl(imageUrl))
            {
                return null;
            }

            var result = new ImageSearchResult();
            var imageBytes = WebHelper.BytesForImageUrl(imageUrl);
            IImageFormat imageFormat = null;
            using (var image = SixLabors.ImageSharp.Image.Load(imageBytes, out imageFormat))
            {
                result.Height = image.Height.ToString();
                result.Width = image.Width.ToString();
                result.MediaUrl = imageUrl;
            }

            return result;
        }

        public static bool IsArtistImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }

            return Regex.IsMatch(fileinfo.Name,
                                @"(band|artist|group|photo)\.(jpg|jpeg|png|bmp|gif)",
                                RegexOptions.IgnoreCase);
        }

        public static bool IsArtistSecondaryImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }

            return Regex.IsMatch(fileinfo.Name,
                                @"(artist_logo|logo|photo[-_\s]*[0-9]+|(artist[\s_-]+[0-9]+)|(band[\s_-]+[0-9]+))\.(jpg|jpeg|png|bmp|gif)",
                                RegexOptions.IgnoreCase);
        }

        public static bool IsImageBetterQuality(string image1, string compareToImage)
        {
            if (string.IsNullOrEmpty(compareToImage))
            {
                return true;
            }
            try
            {
                if (string.IsNullOrEmpty(image1) || !File.Exists(image1))
                {
                    return File.Exists(compareToImage);
                }
                using (var imageComparing = SixLabors.ImageSharp.Image.Load(image1))
                {
                    using (var imageToCompare = SixLabors.ImageSharp.Image.Load(compareToImage))
                    {
                        // Generally a larger image is the preferred image
                        var isBigger = imageToCompare.Height > imageComparing.Height &&
                            imageToCompare.Width > imageComparing.Width;
                        if (isBigger)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error IsImageBetterQuality Image Comparing [{ image1 }] to [{ compareToImage }], Error [{ ex }]",
                                "Warning");
            }
            return false;
        }

        public static bool IsLabelImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }

            return Regex.IsMatch(fileinfo.Name,
                                @"(label|recordlabel|record_label)\.(jpg|jpeg|png|bmp|gif)",
                                RegexOptions.IgnoreCase);
        }

        public static bool IsReleaseImage(FileInfo fileinfo, string releaseName = null)
        {
            if (fileinfo == null)
            {
                return false;
            }

            return Regex.IsMatch(fileinfo.Name,
                                @"((f[-_\s]*[0-9]*)|00|art|big[art]*|cover|cvr|folder|release|front[-_\s]*)\.(jpg|jpeg|png|bmp|gif)",
                                RegexOptions.IgnoreCase);
        }

        public static bool IsReleaseSecondaryImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }

            return Regex.IsMatch(fileinfo.Name,
                                @"((img[\s-_]*[0-9]*[\s-_]*[0-9]*)|(book[let]*[#-_\s(]*[0-9]*-*[0-9]*(\))*)|(encartes[-_\s]*[(]*[0-9]*[)]*)|sc[an]*(.)?[0-9]*|matrix(.)?[0-9]*|(cover[\s_-]*[0-9]+)|back|traycard|jewel case|disc|(.*)[in]*side(.*)|in([side|lay|let|site])*[0-9]*|digipack.?\[?\(?([0-9]*)\]?\)?|cd.?\[?\(?([0-9]*)\]?\)?|(release[\s_-]+[0-9]+))\.(jpg|jpeg|png|bmp|gif)",
                                RegexOptions.IgnoreCase);
        }

        public static models.Image MakeArtistThumbnailImage(IRoadieSettings configuration,
                                                            IHttpContext httpContext,
                                                            Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }

            return MakeThumbnailImage(configuration, httpContext, id.Value, "artist");
        }

        public static models.Image MakeCollectionThumbnailImage(IRoadieSettings configuration,
                                                                IHttpContext httpContext,
                                                                Guid id)
        { return MakeThumbnailImage(configuration, httpContext, id, "collection"); }

        public static models.Image MakeFullsizeImage(IRoadieSettings configuration,
                                                     IHttpContext httpContext,
                                                     Guid id,
                                                     string caption = null)
        {
            return new models.Image($"{httpContext.ImageBaseUrl}/{id}",
                                    caption,
                                    $"{httpContext.ImageBaseUrl}/{id}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
        }

        public static models.Image MakeFullsizeSecondaryImage(IRoadieSettings configuration,
                                                              IHttpContext httpContext,
                                                              Guid id,
                                                              ImageType type,
                                                              int imageId,
                                                              string caption = null)
        {
            if (type == ImageType.ArtistSecondary)
            {
                return new models.Image($"{httpContext.ImageBaseUrl}/artist-secondary/{id}/{imageId}",
                                        caption,
                                        $"{httpContext.ImageBaseUrl}/artist-secondary/{id}/{imageId}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
            }
            return new models.Image($"{httpContext.ImageBaseUrl}/release-secondary/{id}/{imageId}",
                                    caption,
                                    $"{httpContext.ImageBaseUrl}/release-secondary/{id}/{imageId}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
        }

        public static models.Image MakeGenreThumbnailImage(IRoadieSettings configuration,
                                                           IHttpContext httpContext,
                                                           Guid id)
        { return MakeThumbnailImage(configuration, httpContext, id, "genre"); }

        public static models.Image MakeImage(IRoadieSettings configuration,
                                             IHttpContext httpContext,
                                             Guid id,
                                             string type,
                                             IImageSize imageSize)
        { return MakeImage(configuration, httpContext, id, type, imageSize.Width, imageSize.Height); }

        public static models.Image MakeImage(IRoadieSettings configuration,
                                             IHttpContext httpContext,
                                             Guid id,
                                             int width = 200,
                                             int height = 200,
                                             string caption = null,
                                             bool includeCachebuster = false)
        {
            return new models.Image($"{httpContext.ImageBaseUrl}/{id}/{width}/{height}/{(includeCachebuster ? DateTime.UtcNow.Ticks.ToString() : string.Empty)}",
                                    caption,
                                    $"{httpContext.ImageBaseUrl}/{id}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
        }

        public static models.Image MakeImage(IRoadieSettings configuration,
                                             IHttpContext httpContext,
                                             Guid id,
                                             string type,
                                             int? width,
                                             int? height,
                                             string caption = null,
                                             bool includeCachebuster = false)
        {
            if (width.HasValue &&
                height.HasValue &&
                (width.Value != configuration.ThumbnailImageSize.Width ||
                    height.Value != configuration.ThumbnailImageSize.Height))
            {
                return new models.Image($"{httpContext.ImageBaseUrl}/{type}/{id}/{width}/{height}/{(includeCachebuster ? DateTime.UtcNow.Ticks.ToString() : string.Empty)}",
                                       caption,
                                       $"{httpContext.ImageBaseUrl}/{type}/{id}/{configuration.ThumbnailImageSize.Width}/{configuration.ThumbnailImageSize.Height}");
            }

            return new models.Image($"{httpContext.ImageBaseUrl}/{type}/{id}", caption, null);
        }

        public static models.Image MakeLabelThumbnailImage(IRoadieSettings configuration,
                                                           IHttpContext httpContext,
                                                           Guid id)
        { return MakeThumbnailImage(configuration, httpContext, id, "label"); }

        public static models.Image MakeNewImage(IHttpContext httpContext, string type)
        { return new models.Image($"{httpContext.ImageBaseUrl}/{type}.jpg", null, null); }

        public static models.Image MakePlaylistThumbnailImage(IRoadieSettings configuration,
                                                              IHttpContext httpContext,
                                                              Guid id)
        { return MakeThumbnailImage(configuration, httpContext, id, "playlist"); }

        public static models.Image MakeReleaseThumbnailImage(IRoadieSettings configuration,
                                                             IHttpContext httpContext,
                                                             Guid id)
        { return MakeThumbnailImage(configuration, httpContext, id, "release"); }

        public static models.Image MakeThumbnailImage(IRoadieSettings configuration,
                                                      IHttpContext httpContext,
                                                      Guid id,
                                                      string type,
                                                      int? width = null,
                                                      int? height = null,
                                                      bool includeCachebuster = false)
        {
            return MakeImage(configuration,
                             httpContext,
                             id,
                             type,
                             width ?? configuration.ThumbnailImageSize.Width,
                             height ?? configuration.ThumbnailImageSize.Height,
                             null,
                             includeCachebuster);
        }

        public static models.Image MakeTrackThumbnailImage(IRoadieSettings configuration,
                                                           IHttpContext httpContext,
                                                           Guid id)
        { return MakeThumbnailImage(configuration, httpContext, id, "track"); }

        public static models.Image MakeUnknownImage(IHttpContext httpContext, string unknownType = "unknown")
        { return new models.Image($"{httpContext.ImageBaseUrl}/{ unknownType }.jpg"); }

        public static models.Image MakeUserThumbnailImage(IRoadieSettings configuration,
                                                          IHttpContext httpContext,
                                                          Guid id)
        { return MakeThumbnailImage(configuration, httpContext, id, "user"); }

        public static byte[] ResizeImage(byte[] imageBytes, int width, int height) => ImageHelper.ResizeImage(imageBytes,
                                                                                                              width,
                                                                                                              height,
                                                                                                              false)
            .Item2;

        /// <summary>
        /// Resize a given image to given dimensions if needed
        /// </summary>
        /// <param name="imageBytes">Image bytes to resize</param>
        /// <param name="width">Resize to width</param>
        /// <param name="height">Resize to height</param>
        /// <param name="forceResize">Force resize</param>
        /// <returns>Tuple with bool for did resize and byte array of image</returns>
        public static Tuple<bool, byte[]> ResizeImage(byte[] imageBytes,
                                                      int width,
                                                      int height,
                                                      bool? forceResize = false)
        {
            if (imageBytes == null)
            {
                return null;
            }
            try
            {
                using (var outStream = new MemoryStream())
                {
                    var resized = false;
                    IImageFormat imageFormat = null;
                    using (var image = SixLabors.ImageSharp.Image.Load(imageBytes, out imageFormat))
                    {
                        var doForce = forceResize ?? false;
                        if (doForce || image.Width > width || image.Height > height)
                        {
                            int newWidth, newHeight;
                            if (doForce)
                            {
                                newWidth = width;
                                newHeight = height;
                            }
                            else
                            {
                                float aspect = image.Width / (float)image.Height;
                                if (aspect < 1)
                                {
                                    newWidth = (int)(width * aspect);
                                    newHeight = (int)(newWidth / aspect);
                                }
                                else
                                {
                                    newHeight = (int)(height / aspect);
                                    newWidth = (int)(newHeight * aspect);
                                }
                            }
                            image.Mutate(ctx => ctx.Resize(newWidth, newHeight));
                            resized = true;
                        }
                        image.Save(outStream, imageFormat);
                    }
                    return new Tuple<bool, byte[]>(resized, outStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Resizing Image [{ex}]", "Warning");
            }
            return null;
        }
    }
}