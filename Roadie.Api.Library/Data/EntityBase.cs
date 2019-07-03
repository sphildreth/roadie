using Roadie.Library.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    public abstract class EntityBase
    {
        [Column("createdDate")] [Required] public DateTime CreatedDate { get; set; }

        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool? IsLocked { get; set; }

        [Column("lastUpdated")] public DateTime? LastUpdated { get; set; }

        [Column("RoadieId")] [Required] public Guid RoadieId { get; set; }

        [Column("status")] public Statuses? Status { get; set; }

        public EntityBase()
        {
            RoadieId = Guid.NewGuid();
            Status = Statuses.Incomplete;
            CreatedDate = DateTime.UtcNow;
            IsLocked = false;
        }
    }
}