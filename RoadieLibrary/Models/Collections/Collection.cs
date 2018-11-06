using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    public class Collection : EntityModelBase
    {
        public int CollectionCount { get; set; }

        public string CollectionType { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(200)]
        public string Edition { get; set; }

        public DataToken Maintainer { get; set; }

        public IEnumerable<CollectionRelease> Releases { get; set; }

        public Image Thumbnail { get; set; }
    }
}