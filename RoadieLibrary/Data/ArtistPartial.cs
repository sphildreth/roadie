using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Linq;

namespace Roadie.Library.Data
{
    public partial class Artist
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return $"urn:artist:{ Id}";
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:artist_by_id:{ Id }";
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
                return Artist.CacheRegionUrn(this.RoadieId);
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

        public bool IsNew
        {
            get
            {
                return this.Id < 1;
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.Name);
            }
        }

        public string SortNameValue
        {
            get
            {
                return string.IsNullOrEmpty(this.SortName) ? this.Name : this.SortName;
            }
        }

        public string ArtistFileFolder(IRoadieSettings configuration, string destinationRoot)
        {
            return FolderPathHelper.ArtistPath(configuration, this.SortNameValue, destinationRoot);
        }

        public override string ToString()
        {
            return string.Format("Id [{0}], Name [{1}], SortName [{2}], RoadieId [{3}]", this.Id, this.Name, this.SortNameValue, this.RoadieId);
        }
    }
}