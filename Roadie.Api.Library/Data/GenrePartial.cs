using System;

namespace Roadie.Library.Data
{
    public partial class Genre
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:genre:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:genre_by_id:{Id}";
        }
    }
}