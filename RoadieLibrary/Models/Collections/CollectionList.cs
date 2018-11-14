using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    public class CollectionList : EntityInfoModelBase
    {
        public DataToken Collection { get; set; }
        public int? CollectionFoundCount { get; set; }
        public int? CollectionPosition { get; set; }
        public DataToken Release { get; set; }
        public DataToken Artist { get; set; }
        public int? CollectionCount { get; set; }
        public string CollectionType { get; set; }
        public Image Thumbnail { get; set; }
        public int PercentComplete
        {
            get
            {
                if (this.CollectionCount == 0 || this.CollectionFoundCount == 0)
                {
                    return 0;
                }
                return (int)Math.Floor((decimal)this.CollectionFoundCount / (decimal)this.CollectionCount * 100);
            }
        }
    }
}
