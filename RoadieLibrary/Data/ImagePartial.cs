using Microsoft.Net.Http.Headers;
using Roadie.Library.Imaging;
using Roadie.Library.Utility;
using System;
using System.Linq;

namespace Roadie.Library.Data
{
    public partial class Image
    {
        public static string CacheRegionKey(Guid Id)
        {
            return string.Format("urn:image:{0}", Id);
        }

        public string CacheRegion
        {
            get
            {
                return Image.CacheRegionKey(this.RoadieId);
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