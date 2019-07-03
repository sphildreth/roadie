using Roadie.Library.Models.Statistics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    public class Collection : EntityModelBase
    {
        public const string DefaultIncludes = "comments,stats,list";

        public int CollectionCount { get; set; }

        public int? CollectionFoundCount { get; set; }
        public string CollectionType { get; set; }

        public IEnumerable<Comment> Comments { get; set; }
        [MaxLength(4000)] public string Description { get; set; }

        [MaxLength(200)] public string Edition { get; set; }

        public string ListInCSV { get; set; }
        public string ListInCSVFormat { get; set; }
        public DataToken Maintainer { get; set; }
        public Image MediumThumbnail { get; set; }

        public int? MissingReleaseCount
        {
            get
            {
                if (CollectionCount == 0 || (CollectionFoundCount ?? 0) == 0) return null;
                return CollectionCount - CollectionFoundCount;
            }
        }

        [MaxLength(100)] public string Name { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }

        public int PercentComplete
        {
            get
            {
                if (CollectionCount == 0 || (CollectionFoundCount ?? 0) == 0) return 0;
                return (int)Math.Floor((decimal)CollectionFoundCount / CollectionCount * 100);
            }
        }

        public IEnumerable<CollectionRelease> Releases { get; set; }

        public CollectionStatistics Statistics { get; set; }
        public Image Thumbnail { get; set; }
    }
}