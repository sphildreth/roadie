using Roadie.Library.Enums;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
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
        public static string ArtistSecondaryImageFilename = "artist {0}.jpg"; // Replace with counter of image
        public static string ReleaseCoverFilename = "cover.jpg";
        public static string ReleaseSecondaryImageFilename = "release {0}.jpg"; // Replace with counter of image
        public static string LabelImageFilename = "label.jpg";

        public static byte[] ConvertToJpegFormat(byte[] imageBytes)
        {
            if (imageBytes == null)
            {
                return null;
            }
            using (MemoryStream outStream = new MemoryStream())
            {
                IImageFormat imageFormat = null;
                using (Image<Rgba32> image = Image.Load(imageBytes, out imageFormat))
                {
                    image.Save(outStream, ImageFormats.Jpeg);
                }
                return outStream.ToArray();
            }
        }

        public static string[] GetFiles(string path, string[] patterns = null, SearchOption options = SearchOption.TopDirectoryOnly)
        {
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

        public static string[] ImageExtensions()
        {
            return new string[6] { "*.bmp", "*.jpeg", "*.jpe", "*.jpg", "*.png", "*.gif" };
        }

        public static string[] ImageFilesInFolder(string folder, SearchOption searchOption)
        {
            return ImageHelper.GetFiles(folder, ImageHelper.ImageExtensions(), searchOption);
        }

        public static string[] ImageMimeTypes()
        {
            return new string[4] { "image/bmp", "image/jpeg", "image/png", "image/gif" };
        }

        public static ImageSearchResult ImageSearchResultForImageUrl(string imageUrl)
        {
            if (!WebHelper.IsStringUrl(imageUrl))
            {
                return null;
            }
            var result = new ImageSearchResult();
            var imageBytes = WebHelper.BytesForImageUrl(imageUrl);
            IImageFormat imageFormat = null;
            using (Image<Rgba32> image = Image.Load(imageBytes, out imageFormat))
            {
                result.Height = image.Height.ToString();
                result.Width = image.Width.ToString();
                result.MediaUrl = imageUrl;
            }
            return result;
        }

        /// <summary>
        /// Resize a given image to given dimensions
        /// </summary>
        public static byte[] ResizeImage(byte[] imageBytes, int width, int height)
        {
            if(imageBytes == null)
            {
                return null;
            }
            try
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    IImageFormat imageFormat = null;
                    using (Image<Rgba32> image = Image.Load(imageBytes, out imageFormat))
                    {
                        if (image.Width > width || image.Height > height)
                        {
                            image.Mutate(ctx => ctx.Resize(width, height));
                        }
                        image.Save(outStream, imageFormat);
                    }
                    return outStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Resizing Image [{ ex.ToString() }]");
            }
            return null;
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

        public static bool IsArtistImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }
            return Regex.IsMatch(fileinfo.Name, @"(band|artist|group)\.(jpg|jpeg|png|bmp|gif)", RegexOptions.IgnoreCase);
        }

        public static bool IsArtistSecondaryImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }
            return Regex.IsMatch(fileinfo.Name, @"(artist_logo|logo|(artist[\s_-]+[0-9]+))\.(jpg|jpeg|png|bmp|gif)", RegexOptions.IgnoreCase);
        }

        public static bool IsReleaseImage(FileInfo fileinfo, string releaseName = null)
        {
            if (fileinfo == null)
            {
                return false;
            }
            return Regex.IsMatch(fileinfo.Name, @"((f[-_\s]*[0-9]*)|cover|folder|release|front)\.(jpg|jpeg|png|bmp|gif)", RegexOptions.IgnoreCase);
        }

        public static bool IsReleaseSecondaryImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }
            return Regex.IsMatch(fileinfo.Name, @"((book[let]*[-_\s]*[0-9]*)|(encartes[-_\s]*[(]*[0-9]*[)]*)|(cover[\s_-]+[0-9]+)|back|disc|(.*)[in]*side(.*)|inlet|inlay|cd[0-9]*|(release[\s_-]+[0-9]+))\.(jpg|jpeg|png|bmp|gif)", RegexOptions.IgnoreCase);
        }

        public static bool IsLabelImage(FileInfo fileinfo)
        {
            if (fileinfo == null)
            {
                return false;
            }
            return Regex.IsMatch(fileinfo.Name, @"(label|recordlabel|record_label)\.(jpg|jpeg|png|bmp|gif)", RegexOptions.IgnoreCase);
        }

        public static IEnumerable<FileInfo> FindImageTypeInDirectory(DirectoryInfo directory, ImageType type, SearchOption folderSearchOptions = SearchOption.AllDirectories)
        {
            var result = new List<FileInfo>();
            if (directory == null || !directory.Exists)
            {
                return result;
            }
            var imageFilesInFolder = ImageFilesInFolder(directory.FullName, folderSearchOptions);
            if (imageFilesInFolder == null || !imageFilesInFolder.Any())
            {
                return result;
            }
            foreach(var imageFile in imageFilesInFolder)
            {
                var image = new FileInfo(imageFile);
                switch (type)
                {
                    case ImageType.Artist:
                        if(IsArtistImage(image))
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

    }
}