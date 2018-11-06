using System;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseLabel : EntityModelBase
    {
        public string CatalogNumber { get; set; }

        public Label Label { get; set; }
    }
}