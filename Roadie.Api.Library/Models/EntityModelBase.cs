using Mapster;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Roadie.Library.Models
{
    public abstract class EntityModelBase
    {
        private IEnumerable<string> _alternateNamesList;

        private IEnumerable<string> _tagsList;

        private IEnumerable<string> _urlsList;

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual string AlternateNames { get; set; }

        public virtual IEnumerable<string> AlternateNamesList
        {
            get
            {
                if (_alternateNamesList == null)
                {
                    if (!string.IsNullOrEmpty(AlternateNames))
                    {
                        _alternateNamesList = AlternateNames.Split('|');
                    }
                }
                return _alternateNamesList ?? new string[0];
            }
            set => _alternateNamesList = value;
        }

        public virtual DateTime? BeginDate { get; set; }

        [Required] 
        public virtual DateTime? CreatedDate { get; set; }

        public virtual DateTime? EndDate { get; set; }

        /// <summary>
        /// This is the exposed Id for API consumers, not the Database Id.
        /// </summary>
        [AdaptMember("RoadieId")]
        public virtual Guid? Id { get; set; }

        public virtual bool? IsLocked { get; set; }

        public virtual DateTime? LastUpdated { get; set; }

        [MaxLength(250)]
        public virtual string SortName { get; set; }

        public virtual int? Status { get; set; }

        public string StatusVerbose => SafeParser.ToEnum<Statuses>(Status).ToString();

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual string Tags { get; set; }

        public virtual IEnumerable<string> TagsList
        {
            get
            {
                if (_tagsList == null)
                {
                    if (string.IsNullOrEmpty(Tags)) return null;
                    return Tags.Split('|');
                }

                return _tagsList;
            }
            set => _tagsList = value;
        }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual string URLs { get; set; }

        public virtual IEnumerable<string> URLsList
        {
            get
            {
                if (_urlsList == null)
                {
                    if (string.IsNullOrEmpty(URLs)) return null;
                    return URLs.Split('|');
                }

                return _urlsList;
            }
            set => _urlsList = value;
        }

        public virtual bool? UserBookmarked { get; set; }

        public EntityModelBase()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.UtcNow;
            Status = 0;
        }
    }
}