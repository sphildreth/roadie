using Mapster;
using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

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
        public string AlternateNames { get; set; }

        public IEnumerable<string> AlternateNamesList
        {
            get
            {
                if (_alternateNamesList == null)
                    if (!string.IsNullOrEmpty(AlternateNames))
                        _alternateNamesList = AlternateNames.Split('|');
                return _alternateNamesList ?? Enumerable.Empty<string>();
            }
            set => _alternateNamesList = value;
        }

        public DateTime? BeginDate { get; set; }

        [Required] public virtual DateTime? CreatedDate { get; set; }

        public DateTime? EndDate { get; set; }

        [AdaptMember("RoadieId")] public virtual Guid? Id { get; set; }

        public bool? IsLocked { get; set; }

        public DateTime? LastUpdated { get; set; }

        [MaxLength(250)] public string SortName { get; set; }

        public int? Status { get; set; }

        public string StatusVerbose => SafeParser.ToEnum<Statuses>(Status).ToString();

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string Tags { get; set; }

        public IEnumerable<string> TagsList
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
        public string URLs { get; set; }

        public IEnumerable<string> URLsList
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

        public bool UserBookmarked { get; set; }

        public EntityModelBase()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.UtcNow;
            Status = 0;
        }
    }
}