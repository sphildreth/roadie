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
            this.CreatedDate = null;
            this.Id = null;
            this.Status = null;
        }
    }
}