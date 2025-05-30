using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, parameters) =>
                    {
                        // Bypass signature validation for demo purposes
                        return new JwtSecurityToken(token);
                    },
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };

                try
                {
                    var jwtToken = tokenHandler.ReadJwtToken(jwt);
                    var identity = new ClaimsIdentity("jwt");

                    // Standard JWT claims
                    if (jwtToken.Payload.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub))
                        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, sub.ToString()!));
                    if (jwtToken.Payload.TryGetValue(JwtRegisteredClaimNames.Iss, out var iss))
                        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Iss, iss.ToString()!));
                    if (jwtToken.Payload.TryGetValue(JwtRegisteredClaimNames.Aud, out var aud))
                        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Aud, aud.ToString()!));
                    if (jwtToken.Payload.TryGetValue(JwtRegisteredClaimNames.Iat, out var iat))
                        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Iat, iat.ToString()!));
                    if (jwtToken.Payload.TryGetValue(JwtRegisteredClaimNames.Exp, out var exp))
                        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Exp, exp.ToString()!));

                    // Custom user object
                    if (jwtToken.Payload.TryGetValue("user", out var userObj) && userObj is System.Text.Json.JsonElement userElement && userElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var user = Newtonsoft.Json.Linq.JObject.Parse(userElement.GetRawText());
                        // Map user fields to claims
                        if (user.TryGetValue("uid", out var uid))
                            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, uid.ToString()!));
                        if (user.TryGetValue("name", out var name))
                            identity.AddClaim(new Claim(ClaimTypes.Name, name.ToString()!));
                        if (user.TryGetValue("email", out var email))
                            identity.AddClaim(new Claim(ClaimTypes.Email, email.ToString()!));
                        if (user.TryGetValue("is_admin", out var isAdmin))
                            identity.AddClaim(new Claim("is_admin", isAdmin.ToString()!));
                        if (user.TryGetValue("qlnext_user_id", out var qlUserId))
                            identity.AddClaim(new Claim("qlnext_user_id", qlUserId.ToString()!));
                        if (user.TryGetValue("alias", out var alias))
                            identity.AddClaim(new Claim("alias", alias.ToString()!));
                        if (user.TryGetValue("image", out var image))
                            identity.AddClaim(new Claim("image", image.ToString()!));
                        if (user.TryGetValue("status", out var status))
                            identity.AddClaim(new Claim("status", status.ToString()!));
                        if (user.TryGetValue("permissions", out var permissions) && permissions is Newtonsoft.Json.Linq.JArray permsArray)
                        {
                            foreach (var perm in permsArray)
                                identity.AddClaim(new Claim("permission", perm.ToString()!));
                        }
                        if (user.TryGetValue("roles", out var roles) && roles is Newtonsoft.Json.Linq.JArray rolesArray)
                        {
                            foreach (var role in rolesArray)
                                identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()!));
                        }
                    }

                    principal = new ClaimsPrincipal(identity);
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

