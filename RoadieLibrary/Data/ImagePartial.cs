using Microsoft.Net.Http.Headers;
using Roadie.Library.Imaging;
using Roadie.Library.Utility;
using System;
using System.Linq;

namespace Roadie.Library.Data
{
    public partial class Image
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:image:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:image_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return Artist.CacheUrn(this.RoadieId);
            }
        }

        public string CacheRegion
        {
            get
            {
                return Image.CacheRegionUrn(this.RoadieId);
            }
        }

        public string GenerateSignature()
        {
            if (this.Bytes == null || !this.Bytes.Any())
            {
                return null;
            }
            return ImageHasher.AverageHash(this.Bytes).ToString();
        }


    }
}