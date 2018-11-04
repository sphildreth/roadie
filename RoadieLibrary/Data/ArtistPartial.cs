using Microsoft.Extensions.Configuration;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Artist
    {
        public string CacheRegion
        {
            get
            {
                return string.Format("urn:artist:{0}", this.RoadieId);
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

        public bool IsNew
        {
            get
            {
                return this.Id < 1;
            }
        }

        public override string ToString()
        {
            return string.Format("Id [{0}], Name [{1}], SortName [{2}], RoadieId [{3}]", this.Id, this.Name, this.SortNameValue, this.RoadieId);
        }

        public string SortNameValue
        {
            get
            {
                return string.IsNullOrEmpty(this.SortName) ? this.Name : this.SortName;
            }
        }

        public string ArtistFileFolder(IConfiguration configuration, string destinationRoot)
        {
            return FolderPathHelper.ArtistPath(configuration, this.SortNameValue, destinationRoot);
        }
    }
}
