using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData
{
    [Serializable]
    public class ReleaseMediaSearchResult : SearchResultBase
    {
        public short? ReleaseMediaNumber { get; set; }

        public string ReleaseMediaSubTitle { get; set; }

        public short? TrackCount { get; set; }

        public ICollection<TrackSearchResult> Tracks { get; set; }
    }
}
