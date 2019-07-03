using Roadie.Library.Models.Releases;
using System;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    public class CollectionRelease
    {
        public int ListNumber { get; set; }
        public ReleaseList Release { get; set; }
    }
}