using Roadie.Library.Identity;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    public class SubsonicAuthenticateResponse : Response
    {
        public ApplicationUser User { get; set; }
    }
}