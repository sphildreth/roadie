using Microsoft.AspNetCore.Identity;
using Roadie.Library.Data;
using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(ApplicationUser user, UserManager<ApplicationUser> userManager);
    }
}
