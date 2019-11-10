using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using System;
using System.Diagnostics;
using System.IO;

namespace Roadie.Library.Data
{
    public partial class Playlist
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:playlist:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:playlist_by_id:{Id}";
        }

        /// <summary>
        ///     Returns a full file path to the Playlist Image
        /// </summary>
        public string PathToImage(IRoadieSettings configuration, bool makeFolderIfNotExist = false)
        {
            var folder = configuration.PlaylistImageFolder;
            if (!Directory.Exists(folder) && makeFolderIfNotExist)
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"{ (SortName ?? Name).ToFileNameFriendly() } [{ Id }].jpg");
        }

        public override string ToString()
        {
            return $"Id [{Id}], Name [{Name}], RoadieId [{RoadieId}]";
        }
    }
}