using Roadie.Library.Imaging;
using System;
using System.Linq;

namespace Roadie.Library.Data
{
    public partial class Image
    {
        public string CacheKey => Artist.CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:image:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:image_by_id:{Id}";
        }

        public string GenerateSignature()
        {
            if (Bytes == null || !Bytes.Any()) return null;
            return ImageHasher.AverageHash(Bytes).ToString();
        }
    }
}