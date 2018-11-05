using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Data.Models
{
    [Serializable]
    public class ReleaseMedia : EntityModelBase
    {
        public int MediaNumber { get; set; }
        public string SubTitle { get; set; }

        [Required]
        public short TrackCount { get; set; }

        public List<Track> Tracks { get; set; }

    }
}
