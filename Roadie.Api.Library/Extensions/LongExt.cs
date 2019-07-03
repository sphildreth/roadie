using Roadie.Library.Utility;

namespace Roadie.Library.Extensions
{
    public static class LongExt
    {
        public static string ToFileSize(this long? l)
        {
            if (!l.HasValue) return "0";
            return string.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }
    }
}