using Roadie.Library.Models.Releases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class LabelList : EntityInfoModelBase
    {
        public DataToken Label { get; set; }
        public Image Thumbnail { get; set; }
        public int? ArtistCount { get; set; }
        public int? ReleaseCount { get; set; }
        public int? TrackCount { get; set; }
    }
}
