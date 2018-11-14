using Microsoft.AspNetCore.Identity;
using Roadie.Library.Identity;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(ApplicationUser user, UserManager<ApplicationUser> userManager);
    }
}