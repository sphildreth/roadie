using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Models;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("auth")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<AccountController> logger;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ITokenService tokenService;
        private readonly UserManager<ApplicationUser> userManager;        
        private IRoadieSettings RoadieSettings { get; }
        private ICacheManager CacheManager { get; }
        private IAdminService AdminService { get; }

        public AccountController(
           IAdminService adminService,
           UserManager<ApplicationUser> userManager,
           SignInManager<ApplicationUser> signInManager,
           IConfiguration configuration,
           ILogger<AccountController> logger,
           ITokenService tokenService,
           ICacheManager cacheManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.logger = logger;
            this.tokenService = tokenService;
            this.CacheManager = cacheManager;

            this.RoadieSettings = new RoadieSettings();
            configuration.GetSection("RoadieSettings").Bind(this.RoadieSettings);
            this.AdminService = adminService;
        }

        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> CreateToken([FromBody]LoginModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Login user
                    var loginResult = await signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);
                    if (!loginResult.Succeeded)
                    {
                        return BadRequest();
                    }                    
                    var user = await userManager.FindByNameAsync(model.Username);
                    var now = DateTime.UtcNow;
                    user.LastLogin = now;
                    user.LastApiAccess = now;
                    user.LastUpdated = now;
                    await userManager.UpdateAsync(user);
                    var t = await this.tokenService.GenerateToken(user, this.userManager);
                    this.logger.LogInformation($"Successfully authenticated User [{ model.Username}]");
                    this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    var avatarUrl = $"{this.Request.Scheme}://{this.Request.Host}/images/user/{ user.RoadieId }/{ this.RoadieSettings.ThumbnailImageSize.Width }/{ this.RoadieSettings.ThumbnailImageSize.Height }";
                    return Ok(new 
                    {
                        Username = user.UserName,
                        user.Email,
                        user.LastLogin,
                        avatarUrl,
                        Token = t,
                        user.Timeformat,
                        user.Timezone
                    });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Eror in CreateToken");
                    return BadRequest();
                }
            }
            return BadRequest(ModelState);
        }

        [Authorize]
        [HttpPost]
        [Route("refreshtoken")]
        public async Task<IActionResult> RefreshToken()
        {
            var username = User.Identity.Name ??
                User.Claims.Where(c => c.Properties.ContainsKey("unique_name")).Select(c => c.Value).FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(username))
            {
                var user = await userManager.FindByNameAsync(username);
                return Ok(await this.tokenService.GenerateToken(user, this.userManager));
            }
            else
            {
                ModelState.AddModelError("Authentication", "Authentication failed!");
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = registerModel.Username,
                    Email = registerModel.Email
                };

                var identityResult = await this.userManager.CreateAsync(user, registerModel.Password);
                if (identityResult.Succeeded)
                {
                    if(user.Id == 1)
                    {
                        await this.AdminService.DoInitialSetup(user, this.userManager);
                    }
                    await signInManager.SignInAsync(user, isPersistent: false);
                    var t = await this.tokenService.GenerateToken(user, this.userManager);
                    this.logger.LogInformation($"Successfully authenticated User [{ registerModel.Username}]");
                    this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    var avatarUrl = $"{this.Request.Scheme}://{this.Request.Host}/images/user/{ user.RoadieId }/{ this.RoadieSettings.ThumbnailImageSize.Width }/{ this.RoadieSettings.ThumbnailImageSize.Height }";
                    return Ok(new
                    {
                        Username = user.UserName,
                        user.Email,
                        user.LastLogin,
                        avatarUrl,
                        Token = t,
                        user.Timeformat,
                        user.Timezone
                    });
                }
                else
                {
                    return BadRequest(identityResult.Errors);
                }
            }
            return BadRequest(ModelState);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel resetPasswordModel)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = resetPasswordModel.Username,
                    Email = resetPasswordModel.Email,
                    CreatedDate = DateTime.UtcNow
                };

                var identityResult = await this.userManager.ResetPasswordAsync(user, resetPasswordModel.Token, resetPasswordModel.Password);
                if (identityResult.Succeeded)
                {
                    this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return Ok(this.tokenService.GenerateToken(user, this.userManager));
                }
                else
                {
                    return BadRequest(identityResult.Errors);
                }
            }
            return BadRequest(ModelState);
        }
    }
}