using Mapster;
using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class User
    {
        public bool IsEditor { get; set; }
        public bool IsAdmin { get; set; }
        public string UserName { get; set; }
        [AdaptMember("RoadieId")]
        public Guid UserId { get; set; }
        public int Id { get; set; }

    }
}