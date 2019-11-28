using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

        [NotMapped]
        public IEnumerable<Imaging.IImage> Images { get; set; }

        public bool IsNew => Id < 1;

        public bool IsValid => !string.IsNullOrEmpty(Name);

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

        public string ArtistFileFolder(IRoadieSettings configuration, bool createIfNotFound = false)
        {
            return FolderPathHelper.ArtistPath(configuration, Id, SortNameValue, createIfNotFound);
        }

        public override string ToString()
        {
            return $"Id [{ Id }], Status [{ Status }], Name [{ Name }], SortName [{ SortName }], RoadieId [{ RoadieId}]";
        }
    }
}