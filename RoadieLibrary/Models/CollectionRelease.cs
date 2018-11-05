using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Data.Models
{
    [Serializable]
    public class CollectionRelease
    {
        public Collection Collection { get; set; }

        public int ListNumber { get; set; }

    }
}
