using System;

namespace Roadie.Library.Data
{
    public partial class Playlist
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:playlist:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:playlist_by_id:{Id}";
        }

        public override string ToString()
        {
            return $"Id [{Id}], Name [{Name}], RoadieId [{RoadieId}]";
        }
    }
}