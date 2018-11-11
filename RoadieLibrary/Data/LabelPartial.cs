using System;
using System.Linq;

namespace Roadie.Library.Data
{
    public partial class Label
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:label:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:label_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return Label.CacheUrn(this.RoadieId);
            }
        }

        public string CacheRegion
        {
            get
            {
                return Label.CacheRegionUrn(this.RoadieId);
            }
        }

        public string Etag
        {
            get
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    return String.Concat(md5.ComputeHash(System.Text.Encoding.Default.GetBytes(string.Format("{0}{1}", this.RoadieId, this.LastUpdated))).Select(x => x.ToString("D2")));
                }
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.Name);
            }
        }
    }
}