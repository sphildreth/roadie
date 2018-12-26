using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Genre : EntityModelBase
    {
        [MaxLength(100)]
        public string Name { get; set; }
    }
}