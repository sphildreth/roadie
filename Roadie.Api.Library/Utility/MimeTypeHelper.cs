using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Roadie.Library.Utility
{
    public static class MimeTypeHelper
    {
        public static string Mp3Extension = ".mp3";

        public static readonly Dictionary<string, string> AudioMimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { Mp3Extension, "audio/mpeg" },
            { ".m4a", "audio/mp4" },
            { ".aac", "audio/mp4" },
            { ".webma", "audio/webm" },
            { ".wav", "audio/wav" },
            { ".wma", "audio/x-ms-wma" },
            { ".ogg", "audio/ogg" },
            { ".oga", "audio/ogg" },
            { ".opus", "audio/ogg" },
            { ".ac3", "audio/ac3" },
            { ".dsf", "audio/dsf" },
            { ".m4b", "audio/m4b" },
            { ".xsp", "audio/xsp" },
            { ".dsp", "audio/dsp" }
        };

        public static readonly Dictionary<string, string> ImageMimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".tbn", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".tiff", "image/tiff" },
            { ".webp", "image/webp" },
            { ".ico", "image/vnd.microsoft.icon" },
            { ".svg", "image/svg+xml" },
            { ".svgz", "image/svg+xml" }
        };


        public static string Mp3MimeType => AudioMimeTypes[Mp3Extension];

        public static bool IsFileAudioType(string fileName) => IsFileAudioType(new FileInfo(fileName));

        public static bool IsFileAudioType(FileInfo file)
        {
            if(file?.Exists != true)
            {
                return false;
            }
            var ext = file.Extension;
            return AudioMimeTypes.TryGetValue(ext, out _);
        }

        public static bool IsFileImageType(string fileName) => IsFileImageType(new FileInfo(fileName));

        public static bool IsFileImageType(FileInfo file)
        {
            if (file?.Exists != true)
            {
                return false;
            }
            var ext = file.Extension;
            return ImageMimeTypes.TryGetValue(ext, out _);
        }
    }
}
