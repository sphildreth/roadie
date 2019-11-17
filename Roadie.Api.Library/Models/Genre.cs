using Roadie.Library.Models.Statistics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Genre : EntityModelBase
    {
        public const string DefaultIncludes = "stats";

        public Image Thumbnail { get; set; }

        [MaxLength(4000)] public string Description { get; set; }

        public IEnumerable<Comment> Comments { get; set; }
        [MaxLength(100)] public string Name { get; set; }
        [MaxLength(100)] public string NormalizedName { get; set; }
        public ReleaseGroupingStatistics Statistics { get; set; }

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:genre:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:genre_by_id:{Id}";
        }
        public Image MediumThumbnail { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }
    }
}