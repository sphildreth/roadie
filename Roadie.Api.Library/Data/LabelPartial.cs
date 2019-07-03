using System;
using System.Linq;
using System.Security.Cryptography;

namespace Roadie.Library.Data
{
    public partial class Label
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public string Etag
        {
            get
            {
                using (var md5 = MD5.Create())
                {
                    return string.Concat(md5
                        .ComputeHash(
                            System.Text.Encoding.Default.GetBytes(string.Format("{0}{1}", RoadieId, LastUpdated)))
                        .Select(x => x.ToString("D2")));
                }
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(Name);

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:label:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:label_by_id:{Id}";
        }
    }
}