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
        private readonly IConfiguration Configuration;
        private readonly ILogger<AccountController> Logger;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private readonly ITokenService TokenService;
        private readonly UserManager<ApplicationUser> UserManager;        
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
            this.UserManager = userManager;
            this.SignInManager = signInManager;
            this.Configuration = configuration;
            this.Logger = logger;
            this.TokenService = tokenService;
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
                    var loginResult = await SignInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);
                    if (!loginResult.Succeeded)
                    {
                        return BadRequest();
                    }                    
                    var user = await UserManager.FindByNameAsync(model.Username);
                    var now = DateTime.UtcNow;
                    user.LastLogin = now;
                    user.LastApiAccess = now;
                    user.LastUpdated = now;
                    await UserManager.UpdateAsync(user);
                    var t = await this.TokenService.GenerateToken(user, this.UserManager);
                    this.Logger.LogInformation($"Successfully authenticated User [{ model.Username}]");
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
                    this.Logger.LogError(ex, "Eror in CreateToken");
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
                var user = await UserManager.FindByNameAsync(username);
                return Ok(await this.TokenService.GenerateToken(user, this.UserManager));
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
                    RegisteredOn = DateTime.UtcNow,
                    Email = registerModel.Email
                };

                var identityResult = await this.UserManager.CreateAsync(user, registerModel.Password);
                if (identityResult.Succeeded)
                {
                    if(user.Id == 1)
                    {
                        await this.AdminService.DoInitialSetup(user, this.UserManager);
                    }
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    var t = await this.TokenService.GenerateToken(user, this.UserManager);
                    this.Logger.LogInformation($"Successfully created and authenticated User [{ registerModel.Username}]");
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

                var identityResult = await this.UserManager.ResetPasswordAsync(user, resetPasswordModel.Token, resetPasswordModel.Password);
                if (identityResult.Succeeded)
                {
                    this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    return Ok(this.TokenService.GenerateToken(user, this.UserManager));
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