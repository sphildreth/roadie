using System;

namespace Roadie.Library.SearchEngines.MetaData
{
    [Serializable]
    public class LabelSearchResult : SearchResultBase
    {
        public DateTime? EndDate { get; set; }

        public string LabelImageUrl { get; set; }

        public string LabelName { get; set; }

        public string LabelSortName { get; set; }

        public DateTime? StartDate { get; set; }
    }
}
