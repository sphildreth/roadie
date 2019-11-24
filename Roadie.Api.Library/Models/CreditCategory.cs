using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class CreditCategory : EntityModelBase
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(4000)]
        public string Description { get; set; }
    }
}
