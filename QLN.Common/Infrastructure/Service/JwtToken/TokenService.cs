using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QLN.Common.Infrastructure.IService.ITokenService;
using QLN.Common.Infrastructure.Model;
using System.Data;
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

        public async Task<string> GenerateEnrichedAccessToken(ApplicationUser user, DrupalUser drupalUser, DateTime expiry, IList<string>? roles)
        {
            // TODO: Possibly add additional claims from the DrupalUser if needed

            var authClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.PreferredUsername, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.PhoneNumber, user.PhoneNumber ?? string.Empty),
                new(JwtRegisteredClaimNames.Name, drupalUser.Name ?? string.Empty),
                new(JwtRegisteredClaimNames.Picture, drupalUser.Image ?? string.Empty),
                new(JwtRegisteredClaimNames.Nickname, drupalUser.Alias ?? string.Empty),
                new("is_admin", drupalUser.IsAdmin.ToString() ?? "false"),
                //new("user", JsonSerializer.Serialize(drupalUser), JsonClaimValueTypes.Json) // use this one if we want to store the entire DrupalUser object as a JSON claim
                new("user", JsonSerializer.Serialize(drupalUser)) // use this one if we want to store the entire DrupalUser object as a string claim
            };

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    authClaims.Add(new Claim("role", role));
                }
            }

            if (drupalUser.Permissions != null)
            {
                var permissions = drupalUser.Permissions[0].Split(" ");
                string permissionsJson = JsonSerializer.Serialize(permissions);
                authClaims.Add(new Claim("permission", permissionsJson, JsonClaimValueTypes.JsonArray));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: expiry,
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

