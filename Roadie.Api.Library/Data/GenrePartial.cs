using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Genre
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:genre:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:genre_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return Genre.CacheUrn(this.RoadieId);
            }
        }

        public string CacheRegion
        {
            get
            {
                return Genre.CacheRegionUrn(this.RoadieId);
            }
        }
    }
}
