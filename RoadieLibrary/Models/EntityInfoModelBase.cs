using Mapster;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public abstract class EntityInfoModelBase
    {
        [Key]
        [Required]
        [AdaptMember("RoadieId")]
        public Guid Id { get; set; }

        public string CssClass { get; set; }

        [MaxLength(250)]
        public string SortName { get; set; }

        public DateTime? CreatedDateTime { get; set; }
        public string CreatedDate
        {
            get
            {
                return this.CreatedDateTime.HasValue ? this.CreatedDateTime.Value.ToString("s") : null;
            }
        }

        [JsonIgnore]

        public DateTime? LastUpdatedDateTime { get; set; }
        public string LastUpdated
        {
            get
            {
                return this.LastUpdatedDateTime.HasValue ? this.LastUpdatedDateTime.Value.ToString("s") : this.CreatedDateTime.Value.ToString("s");
            }
        }
    }
}