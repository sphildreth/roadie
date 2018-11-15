using Newtonsoft.Json;
using Roadie.Library.Models.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class UserList : EntityInfoModelBase
    {
        public DataToken User { get; set; }
        
        public Image Thumbnail { get; set; }

        public DateTime? Registered { get; set; }

        public DateTime? LastLoginDate { get; set; }

        public DateTime? LastApiAccessDate { get; set; }

        public DateTime? RegisteredDate { get; set; }

        public bool IsEditor { get; set; }

        public bool? IsPrivate { get; set; }

        public UserStatistics Statistics { get; set; }
    }
}
