using Mapster;
using Newtonsoft.Json;
using Roadie.Library.Utility;
using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public abstract class EntityInfoModelBase
    {
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        ///     This is the "id" of the record in the database and is only used during composition, not stored in cache and not
        ///     returned in results.
        /// </summary>
        [AdaptIgnore]
        [JsonIgnore]
        public int DatabaseId { get; set; }

        [Key]
        [Required]
        [AdaptMember("RoadieId")]
        public Guid Id { get; set; }

        public DateTime? LastUpdated { get; set; }

        [MaxLength(250)]
        public virtual string SortName { get; set; }

        /// <summary>
        ///     Random int to sort when Random Request
        /// </summary>
        [AdaptIgnore]
        [JsonIgnore]
        public int RandomSortId { get; set; }

        public EntityInfoModelBase()
        {
            RandomSortId = StaticRandom.Instance.Next();
        }
    }
}