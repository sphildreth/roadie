using System;

namespace Roadie.Library.SearchEngines.MetaData
{
    [Serializable]
    public class ReleaseLabelSearchResult : SearchResultBase
    {
        public DateTime? BeginDate { get; set; }

        public string CatalogNumber { get; set; }

        public DateTime? EndDate { get; set; }

        public LabelSearchResult Label { get; set; }
    }
}
