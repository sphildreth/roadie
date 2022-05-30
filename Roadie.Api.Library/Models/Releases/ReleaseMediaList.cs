using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public sealed class ReleaseMediaList<T> : EntityInfoModelBase
    {
        public short? MediaNumber { get; set; }
        public string SubTitle { get; set; }
        public int? TrackCount { get; set; }
        public IEnumerable<T> Tracks { get; set; } = Enumerable.Empty<T>();
    }
}