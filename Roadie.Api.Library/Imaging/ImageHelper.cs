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

namespace Roadie.Library.Imaging
{
    public static class ImageHelper
    {
        public static string ArtistImageFilename = "artist.jpg";
        public static string ArtistSecondaryImageFilename = "artist {0}.jpg";
        public static string LabelImageFilename = "label.jpg";
        public static int MaximumThumbnailByteSize = 50000;

        // Replace with counter of image
        public static string ReleaseCoverFilename = "cover.jpg";

        public static string ReleaseSecondaryImageFilename = "release {0}.jpg"; // Replace with counter of image

        public static byte[] ConvertToJpegFormat(byte[] imageBytes)
        {
            if (imageBytes == null) return null;
            using (var outStream = new MemoryStream())
            {
                IImageFormat imageFormat = null;
                using (var image = Image.Load(imageBytes, out imageFormat))
                {
                    image.Save(outStream, ImageFormats.Jpeg);
                }
                return outStream.ToArray();
            }
        }

        public static IEnumerable<FileInfo> FindImagesByName(DirectoryInfo directory, string name,
            SearchOption folderSearchOptions = SearchOption.AllDirectories)
        {
            var result = new List<FileInfo>();
            if (directory == null || !directory.Exists || string.IsNullOrEmpty(name)) return result;
            var imageFilesInFolder = ImageFilesInFolder(directory.FullName, folderSearchOptions);
            if (imageFilesInFolder == null || !imageFilesInFolder.Any()) return result;
            if (imageFilesInFolder.Any())
            {
                name = name.ToAlphanumericName();
                foreach (var imageFileInFolder in imageFilesInFolder)
                {
                    var image = new FileInfo(imageFileInFolder);
                    var filenameWithoutExtension = Path.GetFileName(imageFileInFolder).ToAlphanumericName();
                    var imageName = image.Name.ToAlphanumericName();
                    if (imageName.Equals(name) || filenameWithoutExtension.Equals(name)) result.Add(image);
                }
            }

            return result;
        }

        public static IEnumerable<FileInfo> FindImageTypeInDirectory(DirectoryInfo directory, ImageType type,
            SearchOption folderSearchOptions = SearchOption.AllDirectories)
        {
            var result = new List<FileInfo>();
            if (directory == null || !directory.Exists) return result;
            var imageFilesInFolder = ImageFilesInFolder(directory.FullName, folderSearchOptions);
            if (imageFilesInFolder == null || !imageFilesInFolder.Any()) return result;
            foreach (var imageFile in imageFilesInFolder)
            {
                var image = new FileInfo(imageFile);
                switch (type)
                {
                    case ImageType.Artist:
                        if (IsArtistImage(image)) result.Add(image);
                        break;

                    case ImageType.ArtistSecondary:
                        if (IsArtistSecondaryImage(image)) result.Add(image);
                        break;

                    case ImageType.Release:
                        if (IsReleaseImage(image)) result.Add(image);
                        break;

                    case ImageType.ReleaseSecondary:
                        if (IsReleaseSecondaryImage(image)) result.Add(image);
                        break;

                    case ImageType.Label:
                        if (IsLabelImage(image)) result.Add(image);
                        break;
                }
            }

            return result.OrderBy(x => x.Name);
        }

        public static string[] GetFiles(string path, string[] patterns = null,
                            SearchOption options = SearchOption.TopDirectoryOnly)
        {
            if (patterns == null || patterns.Length == 0) return Directory.GetFiles(path, "*", options);
            if (patterns.Length == 1) return Directory.GetFiles(path, patterns[0], options);
            return patterns.SelectMany(pattern => Directory.GetFiles(path, pattern, options)).Distinct().ToArray();
        }

        public static byte[] ImageDataFromUrl(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var dataString = imageUrl.Trim().Replace('-', '+')
                    .Replace("data:image/jpeg;base64,", "")
                    .Replace("data:image/gif;base64,", "")
                    .Replace("data:image/png;base64,", "");
                return Convert.FromBase64String(dataString);
            }

            return null;
        }

        public static string[] ImageExtensions()
        {
            return new string[6] { "*.bmp", "*.jpeg", "*.jpe", "*.jpg", "*.png", "*.gif" };
        }

        public static string[] ImageFilesInFolder(string folder, SearchOption searchOption)
        {
            return GetFiles(folder, ImageExtensions(), searchOption);
        }

        public static string[] ImageMimeTypes()
        {
            return new string[4] { "image/bmp", "image/jpeg", "image/png", "image/gif" };
        }

        public static ImageSearchResult ImageSearchResultForImageUrl(string imageUrl)
        {
            if (!WebHelper.IsStringUrl(imageUrl)) return null;
            var result = new ImageSearchResult();
            var imageBytes = WebHelper.BytesForImageUrl(imageUrl);
            IImageFormat imageFormat = null;
            using (var image = Image.Load(imageBytes, out imageFormat))
            {
                result.Height = image.Height.ToString();
                result.Width = image.Width.ToString();
                result.MediaUrl = imageUrl;
            }

            return result;
        }

        public static bool IsArtistImage(FileInfo fileinfo)
        {
            if (fileinfo == null) return false;
            return Regex.IsMatch(fileinfo.Name, @"(band|artist|group|photo)\.(jpg|jpeg|png|bmp|gif)",
                RegexOptions.IgnoreCase);
        }

        public static bool IsArtistSecondaryImage(FileInfo fileinfo)
        {
            if (fileinfo == null) return false;
            return Regex.IsMatch(fileinfo.Name,
                @"(artist_logo|logo|photo[-_\s]*[0-9]+|(artist[\s_-]+[0-9]+)|(band[\s_-]+[0-9]+))\.(jpg|jpeg|png|bmp|gif)",
                RegexOptions.IgnoreCase);
        }

        public static bool IsLabelImage(FileInfo fileinfo)
        {
            if (fileinfo == null) return false;
            return Regex.IsMatch(fileinfo.Name, @"(label|recordlabel|record_label)\.(jpg|jpeg|png|bmp|gif)",
                RegexOptions.IgnoreCase);
        }

        public static bool IsReleaseImage(FileInfo fileinfo, string releaseName = null)
        {
            if (fileinfo == null) return false;
            return Regex.IsMatch(fileinfo.Name,
                @"((f[-_\s]*[0-9]*)|art|big[art]*|cover|cvr|folder|release|front[-_\s]*)\.(jpg|jpeg|png|bmp|gif)",
                RegexOptions.IgnoreCase);
        }

        public static bool IsReleaseSecondaryImage(FileInfo fileinfo)
        {
            if (fileinfo == null) return false;
            return Regex.IsMatch(fileinfo.Name,
                @"((img[\s-_]*[0-9]*[\s-_]*[0-9]*)|(book[let]*[#-_\s(]*[0-9]*-*[0-9]*(\))*)|(encartes[-_\s]*[(]*[0-9]*[)]*)|sc[an]*(.)?[0-9]*|matrix(.)?[0-9]*|(cover[\s_-]*[0-9]+)|back|traycard|jewel case|disc|(.*)[in]*side(.*)|in([side|lay|let|site])*[0-9]*|cd(.)?[0-9]*|(release[\s_-]+[0-9]+))\.(jpg|jpeg|png|bmp|gif)",
                RegexOptions.IgnoreCase);
        }

        public static byte[] ResizeImage(byte[] imageBytes, int width, int height) => ImageHelper.ResizeImage(imageBytes, width, height, false).Item2;

        /// <summary>
        /// Resize a given image to given dimensions if needed
        /// </summary>
        /// <param name="imageBytes">Image bytes to resize</param>
        /// <param name="width">Resize to width</param>
        /// <param name="height">Resize to height</param>
        /// <param name="forceResize">Force resize</param>
        /// <returns>Tuple with bool for did resize and byte array of image</returns>
        public static Tuple<bool, byte[]> ResizeImage(byte[] imageBytes, int width, int height, bool? forceResize = false)
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
                    using (var image = Image.Load(imageBytes, out imageFormat))
                    {
                        var doForce = forceResize ?? false;
                        if (doForce || image.Width > width || image.Height > height)
                        {
                            int newWidth, newHeight;
                            if(doForce)
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
                Trace.WriteLine($"Error Resizing Image [{ex}]");
            }
            return null;
        }

        /// <summary>
        /// Convert to JPEG and Resize given image to be a thumbnail size, if larger than maximum thumbnail size no image is returned.
        /// </summary>
        public static byte[] ResizeToThumbnail(byte[] imageBytes, IRoadieSettings configuration)
        {
            if(!imageBytes.Any())
            {
                return imageBytes;
            }
            var width = configuration?.ThumbnailImageSize?.Width ?? 80;
            var height = configuration?.ThumbnailImageSize?.Height ?? 80;
            var result = ImageHelper.ResizeImage(ImageHelper.ConvertToJpegFormat(imageBytes), width, height, true).Item2;
            if(result.Length >= ImageHelper.MaximumThumbnailByteSize)
            {
                Trace.WriteLine($"Thumbnail larger than maximum size after resizing to [{configuration.ThumbnailImageSize.Width}x{configuration.ThumbnailImageSize.Height}] Thumbnail Size [{result.Length}]");
                result = new byte[0];
            }
            return result;
        }

        /// <summary>
        ///     Get image data from all sources for either fileanme or MetaData
        /// </summary>
        /// <param name="filename">Name of the File (ie a CUE file)</param>
        /// <param name="metaData">Populated MetaData</param>
        /// <returns></returns>
        public static AudioMetaDataImage GetPictureForMetaData(IRoadieSettings configuration, string filename, AudioMetaData metaData)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(filename), "Invalid Filename");
            SimpleContract.Requires<ArgumentException>(metaData != null, "Invalid MetaData");

            return ImageForFilename(configuration, filename);
        }

        /// <summary>
        ///     Does image exist with the same filename
        /// </summary>
        /// <param name="filename">Name of the File (ie a CUE file)</param>
        /// <returns>Null if not found else populated image</returns>
        public static AudioMetaDataImage ImageForFilename(IRoadieSettings configuration, string filename)
        {
            AudioMetaDataImage imageMetaData = null;

            if (string.IsNullOrEmpty(filename)) return imageMetaData;
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
                    if (pictures != null && pictures.Any())
                    {
                        FileInfo picture = null;
                        // See if there is a "cover" or "front" jpg file if so use it
                        picture = pictures.FirstOrDefault(x =>
                            x.Name.Equals("cover", StringComparison.OrdinalIgnoreCase));
                        if (picture == null)
                            picture = pictures.FirstOrDefault(x =>
                                x.Name.Equals("front", StringComparison.OrdinalIgnoreCase));
                        if (picture == null) picture = pictures.First();
                        if (picture != null)
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
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex, ex.Serialize());
            }

            return imageMetaData;
        }


    }
}