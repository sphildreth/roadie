using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseLabelList : EntityInfoModelBase
    {
        public DataToken Label { get; set; }
        public string CatalogNumber { get; set; }

        [JsonIgnore]

        public DateTime? BeginDatedDateTime { get; set; }
        public string BeginDate
        {
            get
            {
                return this.BeginDatedDateTime.HasValue ? this.BeginDatedDateTime.Value.ToString("s") : null;
            }
        }

        [JsonIgnore]

        public DateTime? EndDatedDateTime { get; set; }
        public string EndDate
        {
            get
            {
                return this.EndDatedDateTime.HasValue ? this.EndDatedDateTime.Value.ToString("s") : null;
            }
        }
    }
}
