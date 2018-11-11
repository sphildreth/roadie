using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Identity;
using models = Roadie.Library.Models.Users;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase :  ODataController
    {
        protected ICacheManager CacheManager { get; }
        protected IConfiguration Configuration { get; }
        protected IRoadieSettings RoadieSettings { get; }
        protected UserManager<ApplicationUser> UserManager { get; }

        protected ILogger _logger;

        public EntityControllerBase(ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            this.CacheManager = cacheManager;
            this.Configuration = configuration;

            this.RoadieSettings = new RoadieSettings();
            this.Configuration.GetSection("RoadieSettings").Bind(this.RoadieSettings);
            this.UserManager = userManager;

        }

        private models.User _currentUser = null;
        protected async Task<models.User> CurrentUserModel()
        {
            if(this._currentUser == null)
            {
                if(this.User.Identity.IsAuthenticated)
                {
                    var user = await this.UserManager.GetUserAsync(User);
                    this._currentUser = user.Adapt<models.User>();
                    this._currentUser.IsAdmin = User.IsInRole("Admin");
                    this._currentUser.IsEditor = User.IsInRole("Editor");
                }
            }
            return this._currentUser;
        }
    }
}