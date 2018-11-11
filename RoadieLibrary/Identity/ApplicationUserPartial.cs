using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Identity
{
    public partial class ApplicationUser
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:user:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:user_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return ApplicationUser.CacheUrn(this.RoadieId);
            }
        }
    }
}
