using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Roadie.Library.Extensions
{
    public static class StringExt
    {
        private static readonly Dictionary<char, string> UnicodeAccents = new Dictionary<char, string>
        {
            {'À', "A"}, {'Á', "A"}, {'Â', "A"}, {'Ã', "A"}, {'Ä', "Ae"}, {'Å', "A"}, {'Æ', "Ae"},
            {'Ç', "C"},
            {'È', "E"}, {'É', "E"}, {'Ê', "E"}, {'Ë', "E"},
            {'Ì', "I"}, {'Í', "I"}, {'Î', "I"}, {'Ï', "I"},
            {'Ð', "Dh"}, {'Þ', "Th"},
            {'Ñ', "N"},
            {'Ò', "O"}, {'Ó', "O"}, {'Ô', "O"}, {'Õ', "O"}, {'Ö', "Oe"}, {'Ø', "Oe"},
            {'Ù', "U"}, {'Ú', "U"}, {'Û', "U"}, {'Ü', "Ue"},
            {'Ý', "Y"},
            {'ß', "ss"},
            {'à', "a"}, {'á', "a"}, {'â', "a"}, {'ã', "a"}, {'ä', "ae"}, {'å', "a"}, {'æ', "ae"},
            {'ç', "c"},
            {'è', "e"}, {'é', "e"}, {'ê', "e"}, {'ë', "e"},
            {'ì', "i"}, {'í', "i"}, {'î', "i"}, {'ï', "i"},
            {'ð', "dh"}, {'þ', "th"},
            {'ñ', "n"},
            {'ò', "o"}, {'ó', "o"}, {'ô', "o"}, {'õ', "o"}, {'ö', "oe"}, {'ø', "oe"},
            {'ù', "u"}, {'ú', "u"}, {'û', "u"}, {'ü', "ue"},
            {'ý', "y"}, {'ÿ', "y"}
        };

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
                        input += delimiter;
                    }
                    input += value;
                }
            }
            return input;
        }

        public static string CleanString(this string input, IRoadieSettings settings, string removeStringsRegex = null)
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
            if (string.IsNullOrEmpty(result))
            {
                return input;
            }
            var rs = removeStringsRegex ?? settings.Processing.RemoveStringsRegex;
            if (!string.IsNullOrEmpty(rs))
            {
                result = Regex.Replace(result, rs, "", RegexOptions.IgnoreCase);
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
            if (string.IsNullOrEmpty(input)) return false;
            var firstPart = input.Split(' ').First().SafeReplace("[").SafeReplace("]");
            return SafeParser.ToNumber<long>(firstPart) > 0;
        }

        public static string FromHexString(this string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);

            return System.Text.Encoding.UTF8.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
        }

        public static bool IsValidFilename(this string input)
        {
            var containsABadCharacter = new Regex("[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]");
            if (containsABadCharacter.IsMatch(input)) return false;
            ;
            return true;
        }

        public static bool IsValueInDelimitedList(this string input, string value, char delimiter = '|')
        {
            if (string.IsNullOrEmpty(input)) return false;
            var p = input.Split(delimiter);
            return !p.Any() ? false : p.Any(x => x.Trim().Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        public static string NormalizeName(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = input.ToLower();
            var removeParts = new List<string> { " ft. ", " ft ", " feat ", " feat. " };
            foreach (var removePart in removeParts) input = input.Replace(removePart, "");
            var cultInfo = new CultureInfo("en-US", false).TextInfo;
            return cultInfo.ToTitleCase(input).Trim();
        }

        public static string Or(this string input, string alternative)
        {
            return string.IsNullOrEmpty(input) ? alternative : input;
        }

        public static string RemoveDiacritics(this string s)
        {
            var normalizedString = s.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < normalizedString.Length; i++)
            {
                var c = normalizedString[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        public static string RemoveFirst(this string input, string remove = "")
        {
            if (string.IsNullOrEmpty(input)) return input;
            var index = input.IndexOf(remove);
            return index < 0
                ? input
                : input.Remove(index, remove.Length).Trim();
        }

        public static string RemoveStartsWith(this string input, string remove = "")
        {
            if (string.IsNullOrEmpty(input)) return input;
            var index = input.IndexOf(remove);
            var result = input;
            while (index == 0)
            {
                result = result.Remove(index, remove.Length).Trim();
                index = result.IndexOf(remove);
            }

            return result;
        }

        public static string RemoveUnicodeAccents(this string text)
        {
            return text.Aggregate(
                new StringBuilder(),
                (sb, c) =>
                {
                    string r;
                    if (UnicodeAccents.TryGetValue(c, out r)) return sb.Append(r);
                    return sb.Append(c);
                }).ToString();
        }

        public static string ReplaceLastOccurrence(this string input, string find, string replace = "")
        {
            if (string.IsNullOrEmpty(input)) return input;
            var Place = input.LastIndexOf(find);
            return input.Remove(Place, find.Length).Insert(Place, replace);
        }

        public static string SafeReplace(this string input, string replace, string replaceWith = " ")
        {
            if (string.IsNullOrEmpty(input)) return null;
            return input.Replace(replace, replaceWith);
        }

        public static string ScrubHtml(this string value)
        {
            var step1 = Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
            var step2 = Regex.Replace(step1, @"\s{2,}", " ");
            return step2;
        }

        public static string StripStartingNumber(this string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            if (input.DoesStartWithNumber()) return string.Join(" ", input.Split(' ').Skip(1));
            return input;
        }

        public static string ToAlphanumericName(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = WebUtility.HtmlDecode(input);
            input = input.ScrubHtml().ToLower().Trim().Replace("&", "and");
            var arr = input.ToCharArray();
            arr = Array.FindAll(arr, c => char.IsLetterOrDigit(c));
            input = new string(arr).RemoveDiacritics().RemoveUnicodeAccents().Translit();
            input = Regex.Replace(input, @"[^A-Za-z0-9]+", "");
            return input;
        }

        public static string ToContentDispositionFriendly(this string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            return input.Replace(',', ' ');
        }

        public static string ToFileNameFriendly(this string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            return Regex.Replace(PathSanitizer.SanitizeFilename(input, ' '), @"\s+", " ").Trim();
        }

        public static string ToFolderNameFriendly(this string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            return Regex.Replace(PathSanitizer.SanitizeFilename(input, ' '), @"\s+", " ").Trim().TrimEnd('.');
        }

        public static string ToHexString(this string str)
        {
            var sb = new StringBuilder();

            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            foreach (var t in bytes) sb.Append(t.ToString("X2"));

            return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
        }

        public static IEnumerable<string> ToListFromDelimited(this string input, char delimiter = '|')
        {
            if (string.IsNullOrEmpty(input)) return new string[0];
            return input.Split(delimiter);
        }

        public static string ToTitleCase(this string input, bool doPutTheAtEnd = true)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            input = input.Replace("’", "'");
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            var r = textInfo.ToTitleCase(input.Trim().ToLower());
            r = Regex.Replace(r, @"\s+", " ");
            if (doPutTheAtEnd)
            {
                if (r.StartsWith("The "))
                {
                    r = r.Replace("The ", "") + ", The";
                }
            }
            return r.NameCase();
            //return r; // 10/29
        }

        public static int? ToTrackDuration(this string input)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(input.Replace(":", ""))) return null;
            try
            {
                var parts = input.Contains(":") ? input.Split(':').ToList() : new List<string> { input };
                while (parts.Count() < 3) parts.Insert(0, "00:");
                var tsRaw = string.Empty;
                foreach (var part in parts)
                {
                    if (tsRaw.Length > 0) tsRaw += ":";
                    tsRaw += part.PadLeft(2, '0').Substring(0, 2);
                }

                var ts = TimeSpan.MinValue;
                var success = TimeSpan.TryParse(tsRaw, out ts);
                if (success) return (int?)ts.TotalMilliseconds;
            }
            catch
            {
            }

            return null;
        }

        public static string Translit(this string str)
        {
            string[] lat_up =
            {
                "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T",
                "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "\"", "Y", "'", "E", "Yu", "Ya"
            };
            string[] lat_low =
            {
                "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t",
                "u", "f", "kh", "ts", "ch", "sh", "shch", "\"", "y", "'", "e", "yu", "ya"
            };
            string[] rus_up =
            {
                "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У",
                "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я"
            };
            string[] rus_low =
            {
                "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у",
                "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я"
            };
            for (var i = 0; i <= 32; i++)
            {
                str = str.Replace(rus_up[i], lat_up[i]);
                str = str.Replace(rus_low[i], lat_low[i]);
            }

            return str;
        }

        public static string TrimEnd(this string input, string suffixToRemove)
        {
            if (input != null && suffixToRemove != null && input.EndsWith(suffixToRemove))
                return input.Substring(0, input.Length - suffixToRemove.Length);
            return input;
        }

        public static string LastSegmentInUrl(this string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return null;
            }
            var uri = new Uri(input);
            return uri.Segments.Last();
        }

        public static string ToCSV(this IEnumerable<string> input)
        {
            if(input == null || !input.Any())
            {
                return null;
            }
            return string.Join(",", input);
        }
    }
}