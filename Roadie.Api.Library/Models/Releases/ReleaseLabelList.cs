using System;
using System.Text.Json.Serialization;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public sealed class ReleaseLabelList : EntityInfoModelBase
    {
        public string BeginDate => BeginDatedDateTime.HasValue ? BeginDatedDateTime.Value.ToString("s") : null;

        [JsonIgnore] 
        public DateTime? BeginDatedDateTime { get; set; }

        public string CatalogNumber { get; set; }

        public string EndDate => EndDatedDateTime.HasValue ? EndDatedDateTime.Value.ToString("s") : null;

        [JsonIgnore] 
        public DateTime? EndDatedDateTime { get; set; }

        public DataToken Label { get; set; }
    }
}