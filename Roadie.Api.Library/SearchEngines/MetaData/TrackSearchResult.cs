using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData
{
    [Serializable]
    public class TrackSearchResult : SearchResultBase
    {
        public ArtistSearchResult Artist { get; set; }

        public IEnumerable<string> Artists { get; set; }

        public int? Duration { get; set; }

        public string ISRC { get; set; }

        public string Title { get; set; }

        public short? TrackNumber { get; set; }

        public string TrackType { get; set; }

        public TrackSearchResult()
        {
            Artists = new string[0];
        }
    }
}