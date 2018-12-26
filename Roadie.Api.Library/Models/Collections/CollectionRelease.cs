using Roadie.Library.Models.Releases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    public class CollectionRelease
    {
        public ReleaseList Release { get; set; }
        public int ListNumber { get; set; }
    }
}
