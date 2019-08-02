using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using System;
using System.IO;

namespace Roadie.Library.Data
{
    public partial class Genre
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        /// <summary>
        ///     Returns a full file path to the Genre Image
        /// </summary>
        public string PathToImage(IRoadieSettings configuration)
        {
            return Path.Combine(configuration.GenreImageFolder, $"{ Name.ToFileNameFriendly() } [{ Id }].jpg");
        }

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:genre:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:genre_by_id:{Id}";
        }
    }
}