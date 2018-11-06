using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public abstract class UserRatingBase
    {
        public bool IsDisliked { get; set; }
        public bool IsFavorite { get; set; }
        public int? Rating { get; set; }
    }
}