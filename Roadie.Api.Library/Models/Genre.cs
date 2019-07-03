using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Genre : EntityModelBase
    {
        public IEnumerable<Comment> Comments { get; set; }
        [MaxLength(100)] public string Name { get; set; }
    }
}