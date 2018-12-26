using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class UserArtist : UserRatingBase
    {
        public DataToken Artist { get; set; }
        public DataToken User { get; set; }
    }
}