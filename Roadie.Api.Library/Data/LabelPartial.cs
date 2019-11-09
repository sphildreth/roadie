using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Roadie.Library.Data
{
    public partial class Label
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

        public string SortNameValue => string.IsNullOrEmpty(SortName) ? Name : SortName;

        /// <summary>
        ///     Returns a full file path to the Label Image
        /// </summary>
        public string PathToImage(IRoadieSettings configuration)
        {
            return Path.Combine(FolderPathHelper.LabelPath(configuration, SortNameValue), $"{ SortNameValue.ToFileNameFriendly() } [{ Id }].jpg");
        }

        /// <summary>
        ///     Returns a full file path to the Label Image
        /// </summary>
        [Obsolete("This is only here for migration will be removed in future release.")]
        public string OldPathToImage(IRoadieSettings configuration)
        {
            return Path.Combine(configuration.LabelImageFolder, $"{ SortNameValue.ToFileNameFriendly() } [{ Id }].jpg");
        }

        public bool IsValid => !string.IsNullOrEmpty(Name);

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:label:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:label_by_id:{Id}";
        }

        public override string ToString()
        {
            return $"Id [{Id}], Name [{Name}], RoadieId [{RoadieId}]";
        }
    }
}