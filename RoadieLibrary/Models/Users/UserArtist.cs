using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class UserArtist : UserRatingBase
    {
        public Guid Artist { get; set; }
        public Guid UserId { get; set; }
    }
}