using Roadie.Library.Enums;
using System;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseInCollection
    {
        public DataToken Collection { get; set; }

        public Image CollectionImage { get; set; }
        public CollectionType? CollectionType { get; set; }
        public int ListNumber { get; set; }
    }
}