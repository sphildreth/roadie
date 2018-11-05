using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Data.Models
{
    [Serializable]
    public class ReleaseGenre : EntityModelBase
    {
        public Genre Genre { get; set; }
    }
}
