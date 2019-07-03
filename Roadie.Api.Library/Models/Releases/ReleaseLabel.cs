using System;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseLabel : EntityModelBase
    {
        public string CatalogNumber { get; set; }

        public LabelList Label { get; set; }

        public ReleaseLabel()
        {
            CreatedDate = null;
            Id = null;
            Status = null;
        }
    }
}