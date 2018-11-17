using Mapster;
using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class User
    {        
        public const string ActionKeyUserRated = "__userrated__";

        public bool IsEditor { get; set; }
        public bool IsAdmin { get; set; }
        public string UserName { get; set; }
        [AdaptMember("RoadieId")]
        public Guid UserId { get; set; }
        public int Id { get; set; }
        public int? PlayerTrackLimit { get; set; }
        public int? RecentlyPlayedLimit { get; set; }
        public int? RandomReleaseLimit { get; set; }
        public bool IsPrivate { get; set; }

        public override string ToString()
        {
            return $"Id [{ Id }], RoadieId [{ UserId }], UserName [{ UserName }]";
        }
    }
}