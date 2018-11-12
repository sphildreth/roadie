using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Roadie.Library.Extensions
{
    public static class StringExt
    {
        public static string AddToDelimitedList(this string input, IEnumerable<string> values, char delimiter = '|')
        {
            if (string.IsNullOrEmpty(input) && (values == null || !values.Any()))
            {
                return null;
            }
            if (string.IsNullOrEmpty(input))
            {
                return string.Join(delimiter.ToString(), values);
            }
            if (values == null || !values.Any())
            {
                return input;
            }
            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }
                if (!input.IsValueInDelimitedList(value, delimiter))
                {
                    if (!input.EndsWith(delimiter.ToString()))
                    {
                        input = input + delimiter;
                    }
                    input = input + value;
                }
            }
            return input;
        }

        public static string CleanString(this string input, IRoadieSettings settings)
        {
            if (string.IsNullOrEmpty(input) || settings == null)
            {
                return input;
            }
            var result = input;
            foreach (var kvp in settings.Processing.ReplaceStrings.OrderBy(x => x.Order).ThenBy(x => x.Key))
            {
                result = result.Replace(kvp.Key, kvp.ReplaceWith, StringComparison.OrdinalIgnoreCase);
            }
            result = result.Trim().ToTitleCase(false);
            var removeStringsRegex = settings.Processing.RemoveStringsRegex;
            if (!string.IsNullOrEmpty(removeStringsRegex))
            {
                var regexParts = removeStringsRegex.Split('|');
                foreach (var regexPart in regexParts)
                {
                    result = Regex.Replace(result, regexPart, "");
                }
            }
            if (result.Length > 5)
            {
                var extensionStart = result.Substring(result.Length - 5, 2);
                if (extensionStart == "_." || extensionStart == " .")
                {
                    var inputSb = new StringBuilder(result);
                    inputSb.Remove(result.Length - 5, 2);
                    inputSb.Insert(result.Length - 5, ".");
                    result = inputSb.ToString();
                }
            }
            // Strip out any more than single space and remove any blanks before and after
            return Regex.Replace(result, @"\s+", " ").Trim();
        }

        public static bool DoesStartWithNumber(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            var firstPart = input.Split(' ').First().SafeReplace("[").SafeReplace("]");
            return SafeParser.ToNumber<long>(firstPart) > 0;
        }

        public static string FromHexString(this string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return System.Text.Encoding.UTF8.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
        }

        public static bool IsValidFilename(this string input)
        {
            Regex containsABadCharacter = new Regex("[" + Regex.Escape(new string(System.IO.Path.GetInvalidPathChars())) + "]");
            if (containsABadCharacter.IsMatch(input))
            {
                return false;
            };
            return true;
        }

        public static bool IsValueInDelimitedList(this string input, string value, char delimiter = '|')
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            var p = input.Split(delimiter);
            return !p.Any() ? false : p.Any(x => x.Trim().Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        public static string NormalizeName(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            input = input.ToLower();
            var removeParts = new List<string> { " ft. ", " ft ", " feat ", " feat. " };
            foreach (var removePart in removeParts)
            {
                input = input.Replace(removePart, "");
            }
            TextInfo cultInfo = new CultureInfo("en-US", false).TextInfo;
            return cultInfo.ToTitleCase(input).Trim();
        }

        public static string Or(this string input, string alternative)
        {
            return string.IsNullOrEmpty(input) ? alternative : input;
        }

        public static string RemoveFirst(this string input, string remove = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            int index = input.IndexOf(remove);
            return (index < 0)
                ? input
                : input.Remove(index, remove.Length).Trim();
        }

        public static string RemoveStartsWith(this string input, string remove = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            int index = input.IndexOf(remove);
            string result = input;
            while (index == 0)
            {
                result = result.Remove(index, remove.Length).Trim();
                index = result.IndexOf(remove);
            }
            return result;
        }

        public static string ReplaceLastOccurrence(this string input, string find, string replace = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            int Place = input.LastIndexOf(find);
            return input.Remove(Place, find.Length).Insert(Place, replace);
        }

        public static string SafeReplace(this string input, string replace, string replaceWith = " ")
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            return input.Replace(replace, replaceWith);
        }

        public static string StripStartingNumber(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            if (input.DoesStartWithNumber())
            {
                return string.Join(" ", input.Split(' ').Skip(1));
            }
            return input;
        }

        public static string ToAlphanumericName(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            input = input.ToLower().Trim().Replace("&", "and");
            char[] arr = input.ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c))));
            return new string(arr);
        }

        public static string ToContentDispositionFriendly(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            return input.Replace(',', ' ');
        }

        public static string ToFileNameFriendly(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            return Regex.Replace(PathSanitizer.SanitizeFilename(input, ' '), @"\s+", " ").Trim();
        }

        public static string ToFolderNameFriendly(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            return Regex.Replace(PathSanitizer.SanitizeFilename(input, ' '), @"\s+", " ").Trim().TrimEnd('.');
        }

        public static string ToHexString(this string str)
        {
            var sb = new StringBuilder();

            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
        }

        public static IEnumerable<string> ToListFromDelimited(this string input, char delimiter = '|')
        {
            if (string.IsNullOrEmpty(input))
            {
                return new string[0];
            }
            return input.Split(delimiter);
        }

        public static string ToTitleCase(this string input, bool doPutTheAtEnd = true)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            var r = textInfo.ToTitleCase(input.Trim().ToLower());
            r = Regex.Replace(r, @"\s+", " ");
            if (doPutTheAtEnd)
            {
                if (r.StartsWith("The "))
                {
                    return r.Replace("The ", "") + ", The";
                }
            }
            return r;
        }

        public static int? ToTrackDuration(this string input)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(input.Replace(":", "")))
            {
                return null;
            }
            try
            {
                var parts = input.Contains(":") ? input.Split(':').ToList() : new List<string> { input };
                while (parts.Count() < 3)
                {
                    parts.Insert(0, "00:");
                }
                var tsRaw = string.Empty;
                foreach (var part in parts)
                {
                    if (tsRaw.Length > 0)
                    {
                        tsRaw += ":";
                    }
                    tsRaw += part.PadLeft(2, '0').Substring(0, 2);
                }
                TimeSpan ts = TimeSpan.MinValue;
                var success = TimeSpan.TryParse(tsRaw, out ts);
                if (success)
                {
                    return (int?)ts.TotalMilliseconds;
                }
            }
            catch
            {
            }
            return null;
        }

        public static string TrimEnd(this string input, string suffixToRemove)
        {
            if (input != null && suffixToRemove != null && input.EndsWith(suffixToRemove))
            {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }
            return input;
        }
    }
}