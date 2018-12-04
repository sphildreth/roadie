using Mapster;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Roadie.Library.Models
{
    public abstract class EntityModelBase
    {
        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string AlternateNames { get; set; }

        public IEnumerable<string> AlternateNamesList
        {
            get
            {
                if (string.IsNullOrEmpty(this.AlternateNames))
                {
                    return null;
                }
                return this.AlternateNames.Split('|');
            }
        }

        public DateTime? BeginDate { get; set; }

        [Required]
        public virtual DateTime? CreatedDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Key]
        [Required]
        [AdaptMember("RoadieId")]
        public virtual Guid? Id { get; set; }

        public bool? IsLocked { get; set; }
        public DateTime? LastUpdated { get; set; }

        [MaxLength(250)]
        public string SortName { get; set; }

        public int? Status { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string Tags { get; set; }

        public IEnumerable<string> TagsList
        {
            get
            {
                if (string.IsNullOrEmpty(this.Tags))
                {
                    return null;
                }
                return this.Tags.Split('|');
            }
        }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string URLs { get; set; }

        public IEnumerable<string> URLsList
        {
            get
            {
                if (string.IsNullOrEmpty(this.URLs))
                {
                    return null;
                }
                return this.URLs.Split('|');
            }
        }

        public bool UserBookmarked { get; set; }

        public EntityModelBase()
        {
            this.Id = Guid.NewGuid();
            this.CreatedDate = DateTime.UtcNow;
            this.Status = 0;
        }
    }
}