﻿using Roadie.Library.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    public abstract class EntityBase
    {
        [Column("createdDate")]
        [Required]
        public DateTime CreatedDate { get; set; }

        [Column("id")]
        [Key]
        public int Id { get; set; }

        public bool? IsLocked { get; set; }

        [Column("lastUpdated")]
        [Required]
        public DateTime? LastUpdated { get; set; }

        [Column("roadieId")]
        [Required]
        public Guid RoadieId { get; set; }

        [Column("status", TypeName = "enum")]
        public Statuses Status { get; set; }
    }
}