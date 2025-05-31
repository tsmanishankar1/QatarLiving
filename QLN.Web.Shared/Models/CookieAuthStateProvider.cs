using Google.Api;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    public class CookieAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public CookieAuthStateProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity());

            if (httpContext != null && httpContext.Request.Cookies.TryGetValue("qat", out var jwt) && !string.IsNullOrEmpty(jwt))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, parameters) =>
                    {
                        // Bypass signature validation for demo purposes
                        return new JwtSecurityToken(token);
                    },
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };

                try
                {
                    SecurityToken validatedToken;
                    var validatedPrincipal = tokenHandler.ValidateToken(jwt, validationParameters, out validatedToken);

                    if (validatedToken.ValidTo > DateTime.UtcNow)
                    {

                        var decodedTokenParts = tokenHandler.ReadJwtToken(jwt).ToString().Split('.');
                        var decodedToken = decodedTokenParts.Length > 1
                            ? string.Join(".", decodedTokenParts.Skip(1))
                            : string.Empty;

                        if (!string.IsNullOrEmpty(decodedToken))
                        {

                            var drupalToken = JsonSerializer.Deserialize<DrupalJWTToken>(decodedToken);
                            var identity = (ClaimsIdentity)validatedPrincipal.Identity!;

                            if (drupalToken != null)
                            {
                                // Custom user object
                                if (drupalToken.DrupalUser != null)
                                {
                                    var user = drupalToken.DrupalUser;
                                    if (!string.IsNullOrEmpty(user.Uid))
                                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Uid));
                                    if (!string.IsNullOrEmpty(user.Name))
                                        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                                    if (!string.IsNullOrEmpty(user.Email))
                                        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                                    if (user.IsAdmin != null)
                                        identity.AddClaim(new Claim("is_admin", user.IsAdmin.ToString()!));
                                    if (!string.IsNullOrEmpty(user.QlnextUserId))
                                        identity.AddClaim(new Claim("qlnext_user_id", user.QlnextUserId));
                                    if (!string.IsNullOrEmpty(user.Alias))
                                        identity.AddClaim(new Claim("alias", user.Alias));
                                    if (!string.IsNullOrEmpty(user.Image))
                                        identity.AddClaim(new Claim("image", user.Image));
                                    if (!string.IsNullOrEmpty(user.Status))
                                        identity.AddClaim(new Claim("status", user.Status));
                                    if (user.Permissions != null && user.Permissions.Any())
                                    {
                                        foreach (var perm in user.Permissions)
                                            identity.AddClaim(new Claim("permission", perm));
                                    }
                                    if (user.Roles != null && user.Roles.Any())
                                    {
                                        foreach (var role in user.Roles)
                                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                    }
                                }
                                principal = new ClaimsPrincipal(identity);
                            }

                        }
                        else
                        {
                            // fallback to validatedPrincipal if deserialization fails
                            principal = validatedPrincipal;
                        }
                    }
                }
                catch
                {
                    principal = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }

            return Task.FromResult(new AuthenticationState(principal));
        }
    }
}

