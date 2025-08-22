using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using QLN.ContentBO.WebUI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Handlers
{
    public class CookieAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public CookieAuthStateProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity());

            if (httpContext == null)
            {
                //Console.WriteLine("HttpContext is null");
                return Task.FromResult(new AuthenticationState(principal));
            }

            if (httpContext.Request.Cookies.TryGetValue("qat", out var jwt) && !string.IsNullOrEmpty(jwt))
            {
                //Console.WriteLine("Cookie found: {0}", jwt);

                // Get the JWT configuration
                var jwtIssuer = _configuration["Jwt:Issuer"];
                var jwtAudience = _configuration["Jwt:Audience"];
                var jwtSecretKey = _configuration["Jwt:Key"];

                var keyBytes = Encoding.UTF8.GetBytes(jwtSecretKey.Trim());
                var issuerSigningKey = new SymmetricSecurityKey(keyBytes);

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    IssuerSigningKey = issuerSigningKey,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };

                try
                {
                    SecurityToken validatedToken;
                    var validatedPrincipal = tokenHandler.ValidateToken(jwt, validationParameters, out validatedToken);

                    //Console.WriteLine("JWT Validated: {0}", validatedPrincipal.Identity?.IsAuthenticated);

                    if (validatedToken.ValidTo > DateTime.UtcNow)
                    {
                        var decodedTokenParts = tokenHandler.ReadJwtToken(jwt).ToString().Split('.');
                        var decodedToken = decodedTokenParts.Length > 1
                            ? string.Join(".", decodedTokenParts.Skip(1))
                            : string.Empty;

                        //Console.WriteLine("Decoded Token {0}", decodedToken);

                        if (!string.IsNullOrEmpty(decodedToken))
                        {
                            //Console.WriteLine("Attempting to deserialize token from cookie");
                            try
                            {
                                var drupalToken = JsonSerializer.Deserialize<DrupalJWTToken>(decodedToken);
                                var identity = (ClaimsIdentity)validatedPrincipal.Identity!;

                                if (drupalToken != null)
                                {
                                    //Console.WriteLine("Token deserialized successfully");
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
                                   /*   if (!string.IsNullOrEmpty(user.QlnextUserId))
                                            identity.AddClaim(new Claim("qlnext_user_id", user.QlnextUserId));  Commenting this now for future use as suggested by Grant  */
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
                                        if (user.Roles != null && user.Roles.Count != 0)
                                        {
                                            foreach (var role in user.Roles)
                                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                        }
                                    }

                                    principal = new ClaimsPrincipal(identity);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Extracting token from cookie failed: {0}", ex.Message);
                            }

                            // set the httpclient default headers to the value value of the jwt so we can pass it around -
                            // this should only happen even if deserialization is successful, else anyone you pass this
                            // to may also fail
                            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", jwt);

                            // this appears to only work if we have a common httpclient
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

