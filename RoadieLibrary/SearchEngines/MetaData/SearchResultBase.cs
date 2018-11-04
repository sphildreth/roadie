using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData
{
    public abstract class SearchResultBase
    {
        private string _RoadieId = null;

        public ICollection<string> AlternateNames { get; set; }
        public string AmgId { get; set; }
        public string Bio { get; set; }
        public string DiscogsId { get; set; }
        public ICollection<string> ImageUrls { get; set; }
        public ICollection<string> IPIs { get; set; }
        public ICollection<string> ISNIs { get; set; }
        public string iTunesId { get; set; }
        public string LastFMId { get; set; }
        public string MusicBrainzId { get; set; }
        public string Profile { get; set; }

        public string RoadieId
        {
            get
            {
                return this._RoadieId ?? (this._RoadieId = Guid.NewGuid().ToString());
            }
            set
            {
                this._RoadieId = value;
            }
        }

        public string SpotifyId { get; set; }
        public ICollection<string> Tags { get; set; }
        public ICollection<string> Urls { get; set; }
    }
}