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

        public static string CacheUrnByUsername(string Username)
        {
            return $"urn:user_by_username:{ Username }";
        }

        public string CacheRegion
        {
            get
            {
                return ApplicationUser.CacheRegionUrn(this.RoadieId);
            }
        }

        public string CacheKeyByUsername
        {
            get
            {
                return ApplicationUser.CacheUrnByUsername(this.UserName);
            }
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
