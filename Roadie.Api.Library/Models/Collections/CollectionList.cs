using System;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    public sealed class CollectionList : EntityInfoModelBase
    {
        public DataToken Artist { get; set; }
        public DataToken Collection { get; set; }
        public int? CollectionCount { get; set; }
        public int? CollectionFoundCount { get; set; }
        public int? CollectionPosition { get; set; }
        public string CollectionType { get; set; }
        public bool? IsLocked { get; set; }

        public int PercentComplete
        {
            get
            {
                if (CollectionCount == 0 || CollectionFoundCount == 0) return 0;
                return (int)Math.Floor((decimal)CollectionFoundCount / (decimal)CollectionCount * 100);
            }
        }

        public DataToken Release { get; set; }
        public Image Thumbnail { get; set; }

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
                CollectionType = (collection.CollectionType ?? Enums.CollectionType.Unknown).ToString(),
                CollectionFoundCount = foundCount,
                CreatedDate = collection.CreatedDate,
                IsLocked = collection.IsLocked,
                LastUpdated = collection.LastUpdated,
                Thumbnail = collectionThumbnail
            };
        }
    }
}