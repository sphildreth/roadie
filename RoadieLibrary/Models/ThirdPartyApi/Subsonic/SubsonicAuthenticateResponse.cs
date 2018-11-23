using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    public class SubsonicAuthenticateResponse : Response
    {
        public ApplicationUser User { get; set; }
    }
}
