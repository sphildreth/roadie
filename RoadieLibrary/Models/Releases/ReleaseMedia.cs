using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseMedia : EntityModelBase
    {
        public int MediaNumber { get; set; }
        public string SubTitle { get; set; }

        [Required]
        public short TrackCount { get; set; }

        public List<TrackList> Tracks { get; set; }
    }
}