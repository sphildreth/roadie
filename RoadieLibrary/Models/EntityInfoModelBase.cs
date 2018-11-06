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

        public DateTime? CreatedDate { get; set; }

        public DateTime? LastUpdated { get; set; }
    }
}