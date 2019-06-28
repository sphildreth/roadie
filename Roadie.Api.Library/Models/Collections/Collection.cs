using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Roadie.Library.Enums;
using Roadie.Library.Models.Statistics;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    public class Collection : EntityModelBase
    {
        public const string DefaultIncludes = "comments,stats,list";

        public int CollectionCount { get; set; }

        public string CollectionType { get; set; }

        [MaxLength(4000)]
        public string Description { get; set; }

        [MaxLength(200)]
        public string Edition { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public DataToken Maintainer { get; set; }

        public IEnumerable<CollectionRelease> Releases { get; set; }

        public Image Thumbnail { get; set; }
        public Image MediumThumbnail { get; set; }
        public string ListInCSVFormat { get; set; }
        public string ListInCSV { get; set; }

        public int PercentComplete
        {
            get
            {
                if (this.CollectionCount == 0 || (this.CollectionFoundCount ?? 0) == 0)
                {
                    return 0;
                }
                return (int)Math.Floor((decimal)this.CollectionFoundCount / (decimal)this.CollectionCount * 100);
            }
        }

        public int? MissingReleaseCount
        {
            get
            {
                if (this.CollectionCount == 0 || (this.CollectionFoundCount ?? 0) == 0)
                {
                    return null;
                }
                return this.CollectionCount - this.CollectionFoundCount;
            }
        }

        public int? CollectionFoundCount { get; set; }
        public CollectionStatistics Statistics { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }

        public IEnumerable<Comment> Comments { get; set; } 
    }
}