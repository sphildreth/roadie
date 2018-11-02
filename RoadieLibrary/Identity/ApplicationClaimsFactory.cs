using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Roadie.Library.Identity
{
    public class ApplicationClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        public const string SecurityClaimRoleType = "req-security-claim";

        private readonly ApplicationUserDbContext _applicationUserDbContext = null;

        public ApplicationClaimsFactory(
                ApplicationUserDbContext applicationUserDbContext,
                UserManager<ApplicationUser> userManager,
                RoleManager<ApplicationRole> roleManager,
                IOptions<IdentityOptions> optionsAccessor) : base(userManager, roleManager, optionsAccessor)
        {
            _applicationUserDbContext = applicationUserDbContext;
        }

        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var usersRoles = (from ur in _applicationUserDbContext.UsersInRoles.Where(x => x.UserId == user.Id)
                              join r in _applicationUserDbContext.Roles on ur.UserRoleId equals r.Id
                              select r);
            IEnumerable<Claim> userClaims = null;
            if (usersRoles.Any())
            {
                userClaims = usersRoles.Select(x => new Claim(SecurityClaimRoleType, x.Name));
            }
            return new ClaimsPrincipal(new ClaimsIdentity(userClaims, "Password"));
        }
    }
}