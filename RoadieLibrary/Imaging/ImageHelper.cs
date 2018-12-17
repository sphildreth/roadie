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

namespace Roadie.Library.Imaging
{
    public static class ImageHelper
    {

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

        public static string[] ImageFilesInFolder(string folder)
        {
            return ImageHelper.GetFiles(folder, ImageHelper.ImageExtensions());
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
                        image.Mutate(ctx => ctx.Resize(width, height));
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

    }
}