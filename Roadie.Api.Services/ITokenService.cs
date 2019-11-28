using Microsoft.AspNetCore.Identity;
using Roadie.Library.Identity;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(User user, UserManager<User> userManager);
    }
}