namespace Roadie.Library.Extensions
{
    public static class ShortExt
    {
        public static short? Or(this short? value, short? alternative)
        {
            if (!value.HasValue && !alternative.HasValue) return null;
            return value.HasValue ? value : alternative;
        }

        public static short? TakeLarger(this short? value, short? alternative)
        {
            if (!value.HasValue && !alternative.HasValue) return null;
            if (!value.HasValue && alternative.HasValue) return alternative.Value;
            return value.Value > alternative.Value ? value.Value : alternative.Value;
        }
    }
}