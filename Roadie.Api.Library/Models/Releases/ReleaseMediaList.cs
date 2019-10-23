using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseMediaList : EntityInfoModelBase
    {
        public short? MediaNumber { get; set; }
        public string SubTitle { get; set; }
        public int? TrackCount { get; set; }
        public IEnumerable<TrackList> Tracks { get; set; } = new TrackList[0];
    }
}