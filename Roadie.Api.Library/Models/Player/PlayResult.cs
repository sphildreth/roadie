using Newtonsoft.Json;
using Roadie.Library.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library.Models.Player
{
    [Serializable]
    public class PlayResult
    {
        private string _rawTracks;

        public string RawTracks => _rawTracks ?? (_rawTracks = JsonConvert.SerializeObject(Tracks));

        public string SiteName { get; set; }

        public string TotalTrackTime
        {
            get { return TimeSpan.FromMilliseconds((double)Tracks.Sum(x => x.Duration)).ToString(@"hh\:mm\:ss"); }
        }

        public string TrackCount => Tracks.Count().ToString("D3");

        public List<PlayResultTrack> Tracks { get; set; }

        public PlayResult(IRoadieSettings configuration)
        {
            SiteName = configuration.SiteName;
            Tracks = new List<PlayResultTrack>();
        }
    }
}