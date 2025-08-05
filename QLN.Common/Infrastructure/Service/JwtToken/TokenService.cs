using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QLN.Common.Infrastructure.IService.ITokenService;
using QLN.Common.Infrastructure.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QLN.Common.Infrastructure.Service.JwtTokenService
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _config = config;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<string> GenerateAccessToken(ApplicationUser user)
        {
            var authClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new(ClaimTypes.Role, "BasicUser"),
                new("UserId", user.Id.ToString()),
                new("UserName", user.UserName ?? string.Empty),
                new("Email", user.Email ?? string.Empty),
                new("PhoneNumber", user.PhoneNumber ?? string.Empty),                
            };
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: DateTime.UtcNow.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)

            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateEnrichedAccessToken(ApplicationUser user, DrupalUser drupalUser)
        {
            // TODO: Possibly add additional claims from the DrupalUser if needed

            var authClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new("UserId", user.Id.ToString()),
                new("UserName", user.UserName ?? string.Empty),
                new("Email", user.Email ?? string.Empty),
                new("PhoneNumber", user.PhoneNumber ?? string.Empty),
                new("user", JsonSerializer.Serialize(drupalUser))
            };

            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: DateTime.UtcNow.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)

            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}

