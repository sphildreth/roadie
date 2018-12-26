using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Collections
{
    [Serializable]
    class CollectionReleaseList : EntityInfoModelBase
    {
        public DataToken Release { get; set; }
        private string _listNumber = null;

        public string ListNumber
        {
            get
            {
                return this._listNumber ?? (this._listNumber = this.ListNumberValue.ToString("D4"));
            }
            set
            {
                this._listNumber = value;
            }
        }

        [JsonIgnore]
        public int ListNumberValue { get; set; }

        public string ReleaseThumbnailUrl { get; set; }

        public DataToken Artist { get; set; }

        public string ArtistThumbnailUrl { get; set; }
        
        public short? ReleaseRating { get; set; }
        [JsonIgnore]
        public DateTime? ReleaseDateDateTime { get; set; }

        public string ReleaseYear
        {
            get
            {
                return this.ReleaseDateDateTime.HasValue ? this.ReleaseDateDateTime.Value.ToUniversalTime().ToString("yyyy") : null;
            }
        }
    }
}
