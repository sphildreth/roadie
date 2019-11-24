using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public sealed class CreditList : EntityModelBase
    {
        public ArtistList Artist { get; set; }
        public string CreditName { get; set; }
        public DataToken Category { get; set; }
        public string Description { get; set; }
    }
}
