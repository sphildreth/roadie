using Mapster;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Api.Data.Models
{
    [Serializable]
    public abstract class EntityInfoModelBase
    {
        [Key]
        [Required]
        [AdaptMember("RoadieId")]
        public Guid Id { get; set; }

        [MaxLength(250)]
        public string SortName { get; set; }

    }
}
