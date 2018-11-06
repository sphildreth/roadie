using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class UserList : EntityInfoModelBase
    {
        public DataToken User { get; set; }
        
        public string ThumbnailUrl { get; set; }

        [JsonIgnore]
        public DateTime? RegisteredDateTime { get; set; }
        public string RegisteredOn
        {
            get
            {
                return this.RegisteredDateTime.HasValue ? this.RegisteredDateTime.Value.ToString("s") : null;
            }
        }

        [JsonIgnore]
        public DateTime? LastLoginDateTime { get; set; }
        public string LastLogin
        {
            get
            {
                return this.LastLoginDateTime.HasValue ? this.LastLoginDateTime.Value.ToString("s") : null;
            }
        }

        [JsonIgnore]
        public DateTime? LastApiAccessDateTime { get; set; }
        public string LastApiAccess
        {
            get
            {
                return this.LastApiAccessDateTime.HasValue ? this.LastApiAccessDateTime.Value.ToString("s") : null;
            }
        }
    }
}
