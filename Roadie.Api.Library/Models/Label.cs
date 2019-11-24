using Roadie.Library.Models.Statistics;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Label : EntityModelBase
    {
        public const string DefaultIncludes = "comments,stats";

        [MaxLength(65535)] 
        public string BioContext { get; set; }

        public DateTime? BirthDate { get; set; }

        public IEnumerable<Comment> Comments { get; set; }
        
        [MaxLength(50)] 
        public string DiscogsId { get; set; }

        public decimal? Duration { get; set; }

        public string DurationTime
        {
            get
            {
                if (!Duration.HasValue) return "--:--";
                return new TimeInfo(Duration.Value).ToFullFormattedString();
            }
        }

        public Image MediumThumbnail { get; set; }
        
        [MaxLength(100)] 
        public string MusicBrainzId { get; set; }

        [MaxLength(250)]
        public string Name { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }

        [MaxLength(65535)] 
        public string Profile { get; set; }

        public ReleaseGroupingStatistics Statistics { get; set; }
        public Image Thumbnail { get; set; }
        public string SortNameValue => string.IsNullOrEmpty(SortName) ? Name : SortName;
    }
}