using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using System.Threading.Tasks;
using models = Roadie.Library.Models.Users;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase : ODataController
    {
        protected ILogger _logger;
        private models.User _currentUser = null;
        protected ICacheManager CacheManager { get; }
        protected IConfiguration Configuration { get; }
        protected IRoadieSettings RoadieSettings { get; }
        protected UserManager<ApplicationUser> UserManager { get; }

        public EntityControllerBase(ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            this.CacheManager = cacheManager;
            this.Configuration = configuration;

            this.RoadieSettings = new RoadieSettings();
            this.Configuration.GetSection("RoadieSettings").Bind(this.RoadieSettings);
            this.UserManager = userManager;
        }

        protected async Task<models.User> CurrentUserModel()
        {
            if (this._currentUser == null)
            {
                if (this.User.Identity.IsAuthenticated)
                {
                    var user = await this.UserManager.GetUserAsync(User);
                    this._currentUser = this.UserModelForUser(user);
                }
            }
            return this._currentUser;
        }

        protected models.User UserModelForUser(ApplicationUser user)
        {
            var result = user.Adapt<models.User>();
            result.IsAdmin = User.IsInRole("Admin");
            result.IsEditor = User.IsInRole("Editor");
            return result;
        }
    }
}