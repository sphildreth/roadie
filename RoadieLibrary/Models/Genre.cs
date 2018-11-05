using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Data.Models
{
    [Serializable]
    public class Genre : EntityModelBase
    {
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
