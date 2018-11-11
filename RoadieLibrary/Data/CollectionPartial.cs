using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Collection
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:collection:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:collection_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return Collection.CacheUrn(this.RoadieId);
            }
        }
    }
}
