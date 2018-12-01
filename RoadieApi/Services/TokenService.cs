using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Roadie.Library.Identity;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public async Task<string> GenerateToken(ApplicationUser user, UserManager<ApplicationUser> userManager)
        {
            var utcNow = DateTime.UtcNow;

            var roles = await userManager.GetRolesAsync(user);
            var userRoles = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToArray();

            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new Claim[]
            {
                        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Email, user.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, utcNow.ToString())
            }.Union(userRoles);

            var now = DateTime.UtcNow;
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(this._configuration.GetValue<String>("Tokens:PrivateKey")));
            var signingCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature);

            var jwt = new JwtSecurityToken(
                signingCredentials: signingCredentials,
                claims: claims,
                notBefore: utcNow,
                expires: utcNow.AddSeconds(this._configuration.GetValue<int>("Tokens:Lifetime")),
                audience: this._configuration.GetValue<String>("Tokens:Audience"),
                issuer: this._configuration.GetValue<String>("Tokens:Issuer")
                );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}