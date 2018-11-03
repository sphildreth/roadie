using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData
{
    [Serializable]
    public class ArtistSearchResult : SearchResultBase
    {
        public DateTime? BirthDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? BeginDate { get; set; }
        public ICollection<string> ArtistGenres { get; set; }
        public string ArtistName { get; set; }
        public string ArtistRealName { get; set; }
        public string ArtistSortName { get; set; }
        public string ArtistThumbnailUrl { get; set; }
        public string ArtistType { get; set; }
        public ICollection<ReleaseSearchResult> Releases { get; set; }
    }
}