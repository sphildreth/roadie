using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Data.Models
{
    [Serializable]
    public class Collection : EntityModelBase
    {
        public int CollectionCount { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(200)]
        public string Edition { get; set; }

        public int MaintainerId { get; set; }

        public string CollectionType { get; set; }
    }
}
