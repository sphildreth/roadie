using Newtonsoft.Json;
using System;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    internal class CollectionReleaseList : EntityInfoModelBase
    {
        private string _listNumber;
        public DataToken Artist { get; set; }
        public string ArtistThumbnailUrl { get; set; }

        public string ListNumber
        {
            get => _listNumber ?? (_listNumber = ListNumberValue.ToString("D4"));
            set => _listNumber = value;
        }

        [JsonIgnore] public int ListNumberValue { get; set; }
        public DataToken Release { get; set; }
        [JsonIgnore] public DateTime? ReleaseDateDateTime { get; set; }
        public short? ReleaseRating { get; set; }
        public string ReleaseThumbnailUrl { get; set; }

        public string ReleaseYear => ReleaseDateDateTime.HasValue
            ? ReleaseDateDateTime.Value.ToUniversalTime().ToString("yyyy")
            : null;
    }
}