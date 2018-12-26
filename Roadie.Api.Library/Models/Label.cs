using Roadie.Library.Models.Statistics;
using Roadie.Library.Utility;
using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Label : EntityModelBase
    {
        public const string DefaultIncludes = "stats";

        [MaxLength(65535)]
        public string BioContext { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(50)]
        public string DiscogsId { get; set; }

        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [MaxLength(250)]
        public string Name { get; set; }

        [MaxLength(65535)]
        public string Profile { get; set; }

        public Image Thumbnail { get; set; }

        public Image MediumThumbnail { get; set; }

        public ReleaseGroupingStatistics Statistics { get; set; }

        public decimal? Duration { get; set; }
        public string DurationTime
        {
            get
            {
                if (!this.Duration.HasValue)
                {
                    return "--:--";
                }
                return new TimeInfo(this.Duration.Value).ToFullFormattedString();
            }

        }
    }
}