using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Label
    {
        public string CacheRegion
        {
            get
            {
                return string.Format("urn:label:{0}", this.RoadieId);
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
