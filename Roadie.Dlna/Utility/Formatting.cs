using System;
using System.Text.RegularExpressions;

namespace Roadie.Dlna.Utility
{
    public static class Formatting
    {
        private static readonly Regex respace =
          new Regex(@"[.+]+", RegexOptions.Compiled);

        private static readonly Regex sanitizer = new Regex(
              @"\b(?:the|an?|ein(?:e[rs]?)?|der|die|das)\b",
      RegexOptions.IgnoreCase | RegexOptions.Compiled
      );

        private static readonly Regex trim = new Regex(
          @"\s+|^[._+)}\]-]+|[._+({\[-]+$",
          RegexOptions.Compiled
          );

        private static readonly Regex trimmore =
          new Regex(@"^[^\d\w]+|[^\d\w]+$", RegexOptions.Compiled);

        public static bool Booley(string maybeBoolean)
        {
            if (maybeBoolean == null)
            {
                throw new ArgumentNullException(nameof(maybeBoolean));
            }
            maybeBoolean = maybeBoolean.Trim();
            var sc = StringComparer.CurrentCultureIgnoreCase;
            return sc.Equals("yes", maybeBoolean) || sc.Equals("1", maybeBoolean) || sc.Equals("true", maybeBoolean);
        }

        public static string FormatFileSize(this long size)
        {
            if (size < 900)
            {
                return $"{size} B";
            }
            var ds = size / 1024.0;
            if (ds < 900)
            {
                return $"{ds:F2} KB";
            }
            ds /= 1024.0;
            if (ds < 900)
            {
                return $"{ds:F2} MB";
            }
            ds /= 1024.0;
            if (ds < 900)
            {
                return $"{ds:F3} GB";
            }
            ds /= 1024.0;
            if (ds < 900)
            {
                return $"{ds:F3} TB";
            }
            ds /= 1024.0;
            return $"{ds:F4} PB";
        }

        public static string GetSystemName()
        {
            return System.Environment.MachineName;
        }

        public static string StemCompareBase(this string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var san = trimmore.Replace(
              sanitizer.Replace(name, string.Empty),
              string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(san))
            {
                return name;
            }
            return san.StemNameBase();
        }

        public static string StemNameBase(this string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!name.Contains(" "))
            {
                name = name.Replace('_', ' ');
                if (!name.Contains(" "))
                {
                    name = name.Replace('-', ' ');
                }
                name = respace.Replace(name, " ");
            }
            var ws = name;
            string wsprev;
            do
            {
                wsprev = ws;
                ws = trim.Replace(wsprev.Trim(), " ").Trim();
            } while (wsprev != ws);
            if (string.IsNullOrWhiteSpace(ws))
            {
                return name;
            }
            return ws;
        }
    }
}