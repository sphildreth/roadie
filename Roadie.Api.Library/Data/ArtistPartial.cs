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
                return HashHelper.CreateMD5($"{ RoadieId }{ LastUpdated }");
            }
        }

        public bool IsNew => Id < 1;

        public bool IsValid => !string.IsNullOrEmpty(Name);

        public string SortNameValue => string.IsNullOrEmpty(SortName) ? Name : SortName;

        public string GroupBy => SortNameValue.Substring(0, 1).ToUpper();

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

        public string ArtistFileFolder(IRoadieSettings configuration)
        {
            return FolderPathHelper.ArtistPath(configuration, SortNameValue);
        }

        public override string ToString()
        {
            return $"Id [{ Id }], Status [{ Status }], Name [{ Name }], SortName [{ SortNameValue}], RoadieId [{ RoadieId}]";
        }
    }
}