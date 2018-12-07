using Roadie.Library.Enums;
using Roadie.Library.Models.Collections;
using System;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseInCollection
    {
        public CollectionList Collection { get; set; }
        public int ListNumber { get; set; }
    }
}