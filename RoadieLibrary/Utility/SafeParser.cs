using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library.Utility
{
    public static class SafeParser
    {
        public static T ChangeType<T>(object value)
        {
            var t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return default(T);
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return (T)Convert.ChangeType(value, t);
        }

        /// <summary>
        /// Return a value that is safe to use as a Key value
        /// </summary>
        public static string KeyFriendly(string input)
        {
            return input;
        }

        /// <summary>
        /// Safely return a Boolean for a given Input.
        /// <remarks>Has Additional String Operations</remarks>
        /// </summary>
        public static bool ToBoolean(object input)
        {
            if (input == null)
            {
                return false;
            }
            var t = input as bool?;
            if (t != null)
            {
                return t.Value;
            }
            switch (input.ToString().ToLower())
            {
                case "true":
                case "1":
                case "y":
                case "yes":
                    return true;

                default:
                    return false;
            }
        }

        public static Guid? ToGuid(object input)
        {
            if(input == null)
            {
                return null;
            }
            var i = input.ToString();
            if(i[1] == ':')
            {
                i = i.Substring(2, i.Length - 2);
            }
            if (!Guid.TryParse(i.ToString(), out Guid result))
            {
                return null;
            }
            return result;
        }


        public static DateTime? ToDateTime(object input)
        {
            if (input == null)
            {
                return default(DateTime?);
            }
            try
            {
                DateTime dt = DateTime.MinValue;
                var i = input as string ?? input.ToString();
                if (!string.IsNullOrEmpty(i))
                {
                    i.Replace("-", "/");
                    var parts = i.Contains("/") ? i.Split('/').ToList() : new List<string> { i };
                    while (parts.Count() < 3)
                    {
                        parts.Insert(0, "01");
                    }
                    var tsRaw = string.Empty;
                    foreach (var part in parts)
                    {
                        if (tsRaw.Length > 0)
                        {
                            tsRaw += "/";
                        }
                        tsRaw += part;
                    }
                    DateTime.TryParse(tsRaw, out dt);
                }
                try
                {
                    return ChangeType<DateTime?>(input);
                }
                catch
                {
                }
                return dt != DateTime.MinValue ? (DateTime?)dt : null;
            }
            catch
            {
                return default(DateTime?);
            }
        }

        public static T ToEnum<T>(object input) where T : struct, IConvertible
        {
            if (input == null)
            {
                return default(T);
            }
            Enum.TryParse(input.ToString(), out T r);
            return r;
        }

        /// <summary>
        /// Safely Return a Number For Given Input
        /// </summary>
        public static T ToNumber<T>(object input)
        {
            if (input == null)
            {
                return default(T);
            }
            try
            {
                return ChangeType<T>(input);
            }
            catch
            {
                return default(T);
            }
        }

        public static string ToString(object input, string defaultValue = null)
        {
            defaultValue = defaultValue ?? string.Empty;
            if (input == null)
            {
                return defaultValue;
            }
            var r = input as string;
            if (r != null)
            {
                return r.Trim();
            }
            return defaultValue;
        }

        public static int? ToYear(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            int parsed = -1;
            if (input.Length == 4)
            {
                if (int.TryParse(input, out parsed))
                {
                    return parsed > 0 ? (int?)parsed : null;
                }
            }
            else if (input.Length > 4)
            {
                if (int.TryParse(input.Substring(0, 4), out parsed))
                {
                    return parsed > 0 ? (int?)parsed : null;
                }
            }
            return null;
        }
    }
}