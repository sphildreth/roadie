using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class UserRelease : UserRatingBase
    {
        public DataToken Release { get; set; }
        public DataToken User { get; set; }
    }
}