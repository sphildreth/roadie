using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Roadie.Library.Data
{
    public partial class Artist
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public string Etag
        {
            get
            {
                using (var md5 = MD5.Create())
                {
                    return string.Concat(md5
                        .ComputeHash(
                            System.Text.Encoding.Default.GetBytes(string.Format("{0}{1}", RoadieId, LastUpdated)))
                        .Select(x => x.ToString("D2")));
                }
            }
        }

        public bool IsNew => Id < 1;

        public bool IsValid => !string.IsNullOrEmpty(Name);

        public string SortNameValue => string.IsNullOrEmpty(SortName) ? Name : SortName;

        public static string CacheRegionUrn(Guid Id)
        {
            return $"urn:artist:{Id}";
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:artist_by_id:{Id}";
        }

        public static string CacheUrnByName(string name)
        {
            return $"urn:artist_by_name:{name}";
        }

        public string ArtistFileFolder(IRoadieSettings configuration, string destinationRoot)
        {
            return FolderPathHelper.ArtistPath(configuration, SortNameValue, destinationRoot);
        }

        public override string ToString()
        {
            return string.Format("Id [{0}], Name [{1}], SortName [{2}], RoadieId [{3}]", Id, Name, SortNameValue,
                RoadieId);
        }
    }
}