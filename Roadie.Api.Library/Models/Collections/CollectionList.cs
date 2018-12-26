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

        public static CollectionList FromDataCollection(Data.Collection collection, int foundCount, Image collectionThumbnail)
        {
            return new CollectionList
            {
                DatabaseId = collection.Id,
                Collection = new DataToken
                {
                    Text = collection.Name,
                    Value = collection.RoadieId.ToString()
                },
                Id = collection.RoadieId,
                CollectionCount = collection.CollectionCount,
                CollectionType = (collection.CollectionType ?? Roadie.Library.Enums.CollectionType.Unknown).ToString(),
                CollectionFoundCount = foundCount,
                CreatedDate = collection.CreatedDate,
                LastUpdated = collection.LastUpdated,
                Thumbnail = collectionThumbnail
            };
        }
    }
}
