using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class UserRelease : UserRatingBase
    {
        public Guid ReleaseId { get; set; }
        public Guid UserId { get; set; }
    }
}