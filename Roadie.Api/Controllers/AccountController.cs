using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Http.Extensions;
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
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Roadie.Library.Utility;

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
        private IEmailSender EmailSender { get; }
        private IHttpContext RoadieHttpContext { get; }

        public AccountController(
           IAdminService adminService,
           UserManager<ApplicationUser> userManager,
           SignInManager<ApplicationUser> signInManager,
           IConfiguration configuration,
           ILogger<AccountController> logger,
           ITokenService tokenService,
           ICacheManager cacheManager,
           IEmailSender emailSender,
           IHttpContext httpContext)
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
            this.EmailSender = emailSender;
            this.RoadieHttpContext = httpContext;
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
                    user.LastUpdated = now;
                    await UserManager.UpdateAsync(user);
                    var t = await this.TokenService.GenerateToken(user, this.UserManager);
                    this.Logger.LogInformation($"Successfully authenticated User [{ model.Username}]");
                    if(!user.EmailConfirmed)
                    {
                        try
                        {
                            var code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user);
                            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, Request.Scheme);
                            await this.EmailSender.SendEmailAsync(user.Email, $"Confirm your { this.RoadieSettings.SiteName } email", $"Please confirm your { this.RoadieSettings.SiteName } account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, "Error sending confirmation Email");
                        }
                    }
                    this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    var avatarUrl = $"{ this.RoadieHttpContext.ImageBaseUrl }/user/{ user.RoadieId }/{ this.RoadieSettings.ThumbnailImageSize.Width }/{ this.RoadieSettings.ThumbnailImageSize.Height }";
                    return Ok(new 
                    {
                        Username = user.UserName,
                        RecentLimit = user.RecentlyPlayedLimit,
                        user.RemoveTrackFromQueAfterPlayed,
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
                    this.Logger.LogError(ex, $"Error in CreateToken For User [{ model.Username }]");
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
                    DoUseHtmlPlayer = true,
                    Email = registerModel.Email
                };

                var identityResult = await this.UserManager.CreateAsync(user, registerModel.Password);
                if (identityResult.Succeeded)
                {
                    if(user.Id == 1)
                    {
                        await this.AdminService.DoInitialSetup(user, this.UserManager);
                    }
                    var code = await this.UserManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, Request.Scheme);
                    await this.EmailSender.SendEmailAsync(user.Email, $"Confirm your { this.RoadieSettings.SiteName } email", $"Please confirm your { this.RoadieSettings.SiteName } account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    await SignInManager.SignInAsync(user, isPersistent: false);
                    var t = await this.TokenService.GenerateToken(user, this.UserManager);
                    this.Logger.LogInformation($"Successfully created and authenticated User [{ registerModel.Username}]");
                    this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    var avatarUrl = $"{ this.RoadieHttpContext.ImageBaseUrl }/user/{ user.RoadieId }/{ this.RoadieSettings.ThumbnailImageSize.Width }/{ this.RoadieSettings.ThumbnailImageSize.Height }";
                    return Ok(new
                    {
                        Username = user.UserName,
                        RecentLimit = user.RecentlyPlayedLimit,
                        user.RemoveTrackFromQueAfterPlayed,
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

        [HttpGet("confirmemail")]
        public IActionResult ConfirmEmail(string userid, string code)
        {
            var user = this.UserManager.FindByIdAsync(userid).Result;
            IdentityResult result = this.UserManager.ConfirmEmailAsync(user, code).Result;
            if (result.Succeeded)
            {
                this.Logger.LogInformation("User [{0}] Confirmed Email Successfully", userid);
                return Content($"Email for { this.RoadieSettings.SiteName } account confirmed successfully!");
            }
            else
            {
                return Content("Error while confirming your email!");
            }
        }

        [HttpGet("sendpasswordresetemail")]
        public async Task<IActionResult> SendPasswordResetEmail(string username, string callbackUrl)
        {
            var user = await UserManager.FindByNameAsync(username);
            var token = await this.UserManager.GeneratePasswordResetTokenAsync(user);
            callbackUrl = callbackUrl + "?username=" + username + "&token=" + WebEncoders.Base64UrlEncode(System.Text.Encoding.ASCII.GetBytes(token));
            try
            {
                await this.EmailSender.SendEmailAsync(user.Email, $"Reset your { this.RoadieSettings.SiteName } password", $"A request has been made to reset your password for your { this.RoadieSettings.SiteName } account. To proceed <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>click here</a>.");
                this.Logger.LogInformation("User [{0}] Email [{1}] Requested Password Reset Callback [{2}]", username, user.Email, callbackUrl);
                return Ok();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
            }
            return StatusCode(500);
        }

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordModel resetPasswordModel)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(resetPasswordModel.Username);
                var token = System.Text.Encoding.ASCII.GetString(WebEncoders.Base64UrlDecode(resetPasswordModel.Token));
                var identityResult = await this.UserManager.ResetPasswordAsync(user, token, resetPasswordModel.Password);
                if (identityResult.Succeeded)
                {
                    this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    var avatarUrl = $"{ this.RoadieHttpContext.ImageBaseUrl }/user/{ user.RoadieId }/{ this.RoadieSettings.ThumbnailImageSize.Width }/{ this.RoadieSettings.ThumbnailImageSize.Height }";
                    var t = await this.TokenService.GenerateToken(user, this.UserManager);
                    return Ok(new
                    {
                        Username = user.UserName,
                        RecentLimit = user.RecentlyPlayedLimit,
                        user.RemoveTrackFromQueAfterPlayed,
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
    }
}