using Roadie.Library.Identity;
using Roadie.Library.Models.Statistics;
using System;
using System.Linq;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class UserList : EntityInfoModelBase
    {
        public bool IsEditor { get; set; }
        public bool? IsPrivate { get; set; }
        public DateTime? LastActivity { get; set; }
        public DateTime? LastApiAccessDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? Registered { get; set; }
        public DateTime? RegisteredDate { get; set; }
        public UserStatistics Statistics { get; set; }
        public Image Thumbnail { get; set; }
        public DataToken User { get; set; }

        public static UserList FromDataUser(ApplicationUser user, Image thumbnail)
        {
            return new UserList
            {
                DatabaseId = user.Id,
                Id = user.RoadieId,
                User = new DataToken
                {
                    Text = user.UserName,
                    Value = user.RoadieId.ToString()
                },
                IsEditor = user.UserRoles.Any(x => x.Role.Name == "Editor"),
                IsPrivate = user.IsPrivate,
                Thumbnail = thumbnail,
                CreatedDate = user.CreatedDate,
                LastUpdated = user.LastUpdated,
                RegisteredDate = user.RegisteredOn,
                LastLoginDate = user.LastLogin,
                LastApiAccessDate = user.LastApiAccess
            };
        }
    }
}