using System;

namespace Roadie.Library.SearchEngines.MetaData
{
    [Serializable]
    public class LabelSearchResult : SearchResultBase
    {
        public string LabelName { get; set; }
        public string LabelSortName { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? StartDate { get; set; }
        public string LabelImageUrl { get; set; }
    }
}