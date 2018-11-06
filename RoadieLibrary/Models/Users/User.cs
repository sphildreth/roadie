using System;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class User
    {
        public string UserName { get; set; }
        private Guid UserId { get; set; }
    }
}