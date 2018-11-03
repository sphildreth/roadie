using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData
{
    [Serializable]
    public class ReleaseSearchResult : SearchResultBase
    {
        public ArtistSearchResult Artist { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public ICollection<ReleaseMediaSearchResult> ReleaseMedia { get; set; }
        public List<ReleaseLabelSearchResult> ReleaseLabel { get; set; }
        public string LastFMSummary { get; set; }
        public string ReleaseThumbnailUrl { get; set; }
        public string ReleaseTitle { get; set; }
        public string ReleaseType { get; set; }
        public ICollection<string> ReleaseGenres { get; set; }
    }
}