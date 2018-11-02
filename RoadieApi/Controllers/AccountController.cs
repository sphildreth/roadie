using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Models;
using Roadie.Api.Services;
using Roadie.Library.Identity;
using System;
using System.Linq;
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

        public AccountController(
           UserManager<ApplicationUser> userManager,
           SignInManager<ApplicationUser> signInManager,
           IConfiguration configuration,
           ILogger<AccountController> logger,
           ITokenService tokenService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.logger = logger;
            this.tokenService = tokenService;
        }

        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> CreateToken([FromBody]LoginModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //var u = await userManager.FindByNameAsync(model.Username);
                    //u.PasswordHash = userManager.PasswordHasher.HashPassword(u, model.Password);
                    //u.LastUpdated = DateTime.UtcNow;
                    //await userManager.UpdateAsync(u);

                    // Login user
                    var loginResult = await signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);
                    if (!loginResult.Succeeded)
                    {
                        return BadRequest();
                    }
                    // If successful login return OK with generated token
                    var user = await userManager.FindByNameAsync(model.Username);
                    return Ok(this.tokenService.GenerateToken(user));
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
                return Ok(this.tokenService.GenerateToken(user));
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
                    Email = registerModel.Email,
                    CreatedDate = DateTime.UtcNow
                };

                var identityResult = await this.userManager.CreateAsync(user, registerModel.Password);
                if (identityResult.Succeeded)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return Ok(this.tokenService.GenerateToken(user));
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
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return Ok(this.tokenService.GenerateToken(user));
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