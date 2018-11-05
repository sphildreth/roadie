using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Data.Models
{
    [Serializable]
    public class ReleaseLabel : EntityModelBase
    {
        public string CatalogNumber { get; set; }

        public Label Label { get; set; }

    }
}
