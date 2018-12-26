using Roadie.Library.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Models.Player
{
    [Serializable]
    public class PlayResult
    {
        public List<PlayResultTrack> Tracks { get; set; }
        public string TotalTrackTime
        {
            get
            {
                return TimeSpan.FromMilliseconds((double)this.Tracks.Sum(x => x.Duration)).ToString(@"hh\:mm\:ss");
            }
        }
        public string TrackCount
        {
            get
            {
                return this.Tracks.Count().ToString("D3");
            }
        }


        private string _rawTracks = null;

        public string RawTracks
        {
            get
            {
                return this._rawTracks ?? (this._rawTracks = Newtonsoft.Json.JsonConvert.SerializeObject(this.Tracks));
            }
        }


        public string SiteName { get; set; }

        public PlayResult(IRoadieSettings configuration)
        {
            this.SiteName = configuration.SiteName;
            this.Tracks = new List<PlayResultTrack>();
        }
    }
}
