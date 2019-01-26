using Mapster;
using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
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

        private IEnumerable<string> _alternateNamesList = null;
        public IEnumerable<string> AlternateNamesList
        {
            get
            {
                if (this._alternateNamesList == null)
                {
                    if (string.IsNullOrEmpty(this.AlternateNames))
                    {
                        return null;
                    }
                    return this.AlternateNames.Split('|');
                }
                return this._alternateNamesList;
            }
            set
            {
                this._alternateNamesList = value;
            }
        }

        public DateTime? BeginDate { get; set; }

        [Required]
        public virtual DateTime? CreatedDate { get; set; }

        public DateTime? EndDate { get; set; }

        [AdaptMember("RoadieId")]
        public virtual Guid? Id { get; set; }

        public bool? IsLocked { get; set; }
        public DateTime? LastUpdated { get; set; }

        [MaxLength(250)]
        public string SortName { get; set; }

        public int? Status { get; set; }

        public string StatusVerbose
        {
            get
            {
                return SafeParser.ToEnum<Statuses>(this.Status).ToString();
            }
        }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string Tags { get; set; }

        private IEnumerable<string> _tagsList = null;
        public IEnumerable<string> TagsList
        {
            get
            {
                if (this._tagsList == null)
                {
                    if (string.IsNullOrEmpty(this.Tags))
                    {
                        return null;
                    }
                    return this.Tags.Split('|');
                }
                return this._tagsList;
            }
            set
            {
                this._tagsList = value;
            }
        }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string URLs { get; set; }

        private IEnumerable<string> _urlsList = null;
        public IEnumerable<string> URLsList
        {
            get
            {
                if (this._urlsList == null)
                {
                    if (string.IsNullOrEmpty(this.URLs))
                    {
                        return null;
                    }
                    return this.URLs.Split('|');
                }
                return this._urlsList;
            }
            set
            {
                this._urlsList = value;
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