using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Models;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using Roadie.Library.Utility;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("auth")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> Logger;
        private readonly SignInManager<User> SignInManager;
        private readonly ITokenService TokenService;
        private readonly UserManager<User> UserManager;
        private string _baseUrl;

        private IAdminService AdminService { get; }

        private string BaseUrl
        {
            get
            {
                if (_baseUrl == null)
                {
                    var scheme = Request.Scheme;
                    if (RoadieSettings.UseSSLBehindProxy) scheme = "https";
                    var host = Request.Host;
                    if (!string.IsNullOrEmpty(RoadieSettings.BehindProxyHost))
                        host = new HostString(RoadieSettings.BehindProxyHost);
                    _baseUrl = $"{scheme}://{host}";
                }

                return _baseUrl;
            }
        }

        private ICacheManager CacheManager { get; }

        private IEmailSender EmailSender { get; }

        private IHttpContext RoadieHttpContext { get; }

        private IRoadieSettings RoadieSettings { get; }

        public AccountController(IAdminService adminService, UserManager<User> userManager, SignInManager<User> signInManager,
                                 IConfiguration configuration, ILogger<AccountController> logger, ITokenService tokenService,
                                 ICacheManager cacheManager, IEmailSender emailSender, IHttpContext httpContext)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            Logger = logger;
            TokenService = tokenService;
            CacheManager = cacheManager;

            RoadieSettings = new RoadieSettings();
            configuration.GetSection("RoadieSettings").Bind(RoadieSettings);
            AdminService = adminService;
            EmailSender = emailSender;
            RoadieHttpContext = httpContext;
        }

        [HttpGet("confirmemail")]
        public IActionResult ConfirmEmail(string userid, string code)
        {
            try
            {
                var user = UserManager.FindByIdAsync(userid).Result;
                var result = UserManager.ConfirmEmailAsync(user, code).Result;
                if (result.Succeeded)
                {
                    Logger.LogTrace("User [{0}] Confirmed Email Successfully", userid);
                    return Content($"Email for {RoadieSettings.SiteName} account confirmed successfully!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return Content("Error while confirming your email!");
        }

        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> CreateToken([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Login user
                    var loginResult = await SignInManager.PasswordSignInAsync(model.Username, model.Password, false, false);
                    if (!loginResult.Succeeded)
                    {
                        return BadRequest();
                    }
                    var user = await UserManager.FindByNameAsync(model.Username);
                    var now = DateTime.UtcNow;
                    user.LastLogin = now;
                    user.LastUpdated = now;
                    await UserManager.UpdateAsync(user);
                    var t = await TokenService.GenerateToken(user, UserManager);
                    Logger.LogTrace($"Successfully authenticated User [{model.Username}]");
                    if (!user.EmailConfirmed)
                        try
                        {
                            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                            var callbackUrl = $"{BaseUrl}/auth/confirmemail?userId={user.Id}&code={code}";
                            await EmailSender.SendEmailAsync(user.Email,
                                $"Confirm your {RoadieSettings.SiteName} email",
                                $"Please confirm your {RoadieSettings.SiteName} account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Error sending confirmation Email");
                        }

                    CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                    var avatarUrl = $"{RoadieHttpContext.ImageBaseUrl}/user/{user.RoadieId}/{RoadieSettings.ThumbnailImageSize.Width}/{RoadieSettings.ThumbnailImageSize.Height}";
                    return Ok(new
                    {
                        Username = user.UserName,
                        InstanceName = RoadieSettings.SiteName,
                        RecentLimit = user.RecentlyPlayedLimit,
                        user.RemoveTrackFromQueAfterPlayed,
                        user.Email,
                        user.LastLogin,
                        avatarUrl,
                        Token = t,
                        user.Timeformat,
                        user.Timezone,
                        DefaultRowsPerPage = user.DefaultRowsPerPage ?? RoadieSettings.DefaultRowsPerPage
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error in CreateToken For User [{model.Username}]");
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
            try
            {
                var username = User.Identity.Name ??
                       User.Claims.Where(c => c.Properties.ContainsKey("unique_name")).Select(c => c.Value)
                           .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(username))
                {
                    var user = await UserManager.FindByNameAsync(username);
                    return Ok(await TokenService.GenerateToken(user, UserManager));
                }
                ModelState.AddModelError("Authentication", "Authentication failed!");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return BadRequest(ModelState);
        }

        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var now = DateTime.UtcNow;
                    var user = new User
                    {
                        UserName = registerModel.Username,
                        RegisteredOn = now,
                        LastLogin = now,
                        DoUseHtmlPlayer = true,
                        Email = registerModel.Email
                    };

                    if (RoadieSettings.UseRegistrationTokens)
                    {
                        var tokenValidation = await AdminService.ValidateInviteToken(registerModel.InviteToken);
                        if (!tokenValidation.IsSuccess)
                        {
                            Logger.LogTrace("Invalid Invite Token");
                            return StatusCode((int)HttpStatusCode.BadRequest, new { Title = "Invite Token Is Required" });
                        }
                    }

                    var existinUserByUsername = await UserManager.FindByNameAsync(registerModel.Username);
                    if (existinUserByUsername != null)
                    {
                        return StatusCode((int)HttpStatusCode.BadRequest, new { Title = "User With Username Already Exists!" });
                    }

                    var existingUserByEmail = await UserManager.FindByEmailAsync(registerModel.Email);
                    if (existingUserByEmail != null)
                    {
                        return StatusCode((int)HttpStatusCode.BadRequest, new { Title = "User With Email Already Exists!" });
                    }

                    var identityResult = await UserManager.CreateAsync(user, registerModel.Password);
                    if (identityResult.Succeeded)
                    {
                        if (user.Id == 1) await AdminService.DoInitialSetup(user, UserManager);
                        try
                        {
                            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                            var callbackUrl = $"{BaseUrl}/auth/confirmemail?userId={user.Id}&code={code}";
                            await EmailSender.SendEmailAsync(user.Email, $"Confirm your {RoadieSettings.SiteName} email",
                                $"Please confirm your {RoadieSettings.SiteName} account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"Error Sending Register Email to [{registerModel.Email}]");
                        }

                        await SignInManager.SignInAsync(user, false);
                        var t = await TokenService.GenerateToken(user, UserManager);
                        Logger.LogTrace($"Successfully created and authenticated User [{registerModel.Username}]");
                        CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                        var avatarUrl = $"{RoadieHttpContext.ImageBaseUrl}/user/{user.RoadieId}/{RoadieSettings.ThumbnailImageSize.Width}/{RoadieSettings.ThumbnailImageSize.Height}";
                        if (registerModel.InviteToken.HasValue)
                        {
                            await AdminService.UpdateInviteTokenUsed(registerModel.InviteToken);
                        }
                        return Ok(new
                        {
                            Username = user.UserName,
                            InstanceName = RoadieSettings.SiteName,
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

                    return BadRequest(identityResult.Errors);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error In Register. Model [{0}]", JsonSerializer.Serialize(registerModel));
            }
            return BadRequest(ModelState);
        }

        [HttpGet("registeroptions")]
        public IActionResult RegisterOptions()
        {
            return Ok(new
            {
                RoadieSettings.IsRegistrationClosed,
                RoadieSettings.UseRegistrationTokens
            });
        }

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel resetPasswordModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await UserManager.FindByNameAsync(resetPasswordModel.Username);
                    var token = Encoding.ASCII.GetString(WebEncoders.Base64UrlDecode(resetPasswordModel.Token));
                    var identityResult = await UserManager.ResetPasswordAsync(user, token, resetPasswordModel.Password);
                    if (identityResult.Succeeded)
                    {
                        CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
                        await SignInManager.SignInAsync(user, false);
                        var avatarUrl =
                            $"{RoadieHttpContext.ImageBaseUrl}/user/{user.RoadieId}/{RoadieSettings.ThumbnailImageSize.Width}/{RoadieSettings.ThumbnailImageSize.Height}";
                        var t = await TokenService.GenerateToken(user, UserManager);
                        return Ok(new
                        {
                            Username = user.UserName,
                            InstanceName = RoadieSettings.SiteName,
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

                    return BadRequest(identityResult.Errors);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error In ResetPassword. Model [{0}]", JsonSerializer.Serialize(resetPasswordModel));
            }

            return BadRequest(ModelState);
        }

        [HttpGet("sendpasswordresetemail")]
        public async Task<IActionResult> SendPasswordResetEmail(string username, string callbackUrl)
        {
            try
            {
                var user = await UserManager.FindByNameAsync(username);
                if (user == null)
                {
                    Logger.LogError($"Unable to find user by username [{username}]");
                    return StatusCode(500);
                }

                var token = await UserManager.GeneratePasswordResetTokenAsync(user);
                callbackUrl = callbackUrl + "?username=" + username + "&token=" +
                              WebEncoders.Base64UrlEncode(Encoding.ASCII.GetBytes(token));
                await EmailSender.SendEmailAsync(user.Email, $"Reset your {RoadieSettings.SiteName} password",
                    $"A request has been made to reset your password for your {RoadieSettings.SiteName} account. To proceed <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>click here</a>.");
                Logger.LogTrace("User [{0}] Email [{1}] Requested Password Reset Callback [{2}]", username,
                    user.Email, callbackUrl);
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return StatusCode(500);
        }
    }
}