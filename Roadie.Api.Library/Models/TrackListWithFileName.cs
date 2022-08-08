using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.Models
{
    [Serializable]
    [DebuggerDisplay("Trackid [{ TrackId }], Track Name [{ TrackName }}, Release Name [{ ReleaseName }]")]
    public sealed class TrackListWithFileName : TrackList
    {
        public new int? DatabaseId { get; set; }

        public string FileName { get; set; }

        public string FileHash { get; set; }
    }
}
