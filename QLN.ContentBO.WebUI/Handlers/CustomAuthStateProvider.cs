using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using QLN.ContentBO.WebUI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Handlers
{

    // Helper classes for V2 token structure
    public class V2User
    {
        public string? Status { get; set; }
        public string? Language { get; set; }
        public string? Created { get; set; }
        public string? Access { get; set; }
        public string? Login { get; set; }
        public string? Init { get; set; }
        public string? Timezone { get; set; }
        public string? Uid { get; set; }
        public string? QlnextUserId { get; set; }
        public string? Name { get; set; }
        public string? Alias { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Path { get; set; }
        public string? Image { get; set; }
        public bool? IsAdmin { get; set; }
        public List<string>? Permissions { get; set; }
        public List<string>? Roles { get; set; }
        public string? Subscription { get; set; }
    }

    public class V2Subscription
    {
        public string? UserId { get; set; }
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int Vertical { get; set; }
        public int SubVertical { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }

    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private string accessToken;

        public CustomAuthStateProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            ClaimsPrincipal principal = new(new ClaimsIdentity());

            if (httpContext == null)
            {
                return new AuthenticationState(principal);
            }

            // First check for legacy "qat" token
            if (httpContext.Request.Cookies.TryGetValue("qat", out var qatJWT) && !string.IsNullOrEmpty(qatJWT))
            {
                var tokenHandlerQAT = new JwtSecurityTokenHandler();
                var validationParametersQAT = new TokenValidationParameters
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
                    var validatedPrincipalQAT = tokenHandlerQAT.ValidateToken(qatJWT, validationParametersQAT, out SecurityToken validatedTokenQAT);

                    if (validatedTokenQAT.ValidTo > DateTime.UtcNow)
                    {
                        principal = validatedPrincipalQAT;
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", qatJWT);

                        // Legacy token is valid, Check for V2 token as well
                        if (httpContext.Request.Cookies.TryGetValue("qat_v2", out var qatV2JWT) && !string.IsNullOrEmpty(qatV2JWT))
                        {
                            var tokenHandler = new JwtSecurityTokenHandler();
                            var validationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = false,
                                ValidateIssuerSigningKey = false,
                                SignatureValidator = (token, parameters) => new JwtSecurityToken(token),
                                ValidIssuer = _configuration["Jwt:Issuer"],
                                ValidAudience = _configuration["Jwt:Audience"],
                                RoleClaimType = ClaimTypes.Role,
                                NameClaimType = ClaimTypes.Name
                            };

                            try
                            {
                                SecurityToken validatedToken;
                                var validatedPrincipal = tokenHandler.ValidateToken(qatV2JWT, validationParameters, out validatedToken);

                                if (validatedToken.ValidTo > DateTime.UtcNow)
                                {
                                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", qatV2JWT);

                                    // If Refresh Token is not available, Sync User Data
                                    if (httpContext.Request.Cookies.TryGetValue("qat_v2_refresh", out var refreshToken) && string.IsNullOrEmpty(refreshToken))
                                    {
                                        // Sync user data and update cookies
                                        var configuration = httpContext.RequestServices.GetService<IConfiguration>();
                                        var baseAddress = configuration?["ServiceUrlPaths:ContentBOAPI"] ?? "https://qlc-bo-dev.qatarliving.com";
                                        var userSyncRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseAddress}/auth/sync");
                                        userSyncRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, qatJWT); // Should send qatJWT (Drupal) for sync
                                        var userSyncResponse = await _httpClient.SendAsync(userSyncRequest);

                                        if (userSyncResponse.IsSuccessStatusCode)
                                        {
                                            var json = await userSyncResponse.Content.ReadAsStringAsync();
                                            var result = JsonSerializer.Deserialize<TokenV2Response>(json, new JsonSerializerOptions
                                            {
                                                PropertyNameCaseInsensitive = true
                                            });

                                            // Update cookies
                                            httpContext.Response.Cookies.Append("qat_v2", result.AccessToken, new CookieOptions
                                            {
                                                HttpOnly = true,
                                                Secure = true,
                                            });

                                            httpContext.Response.Cookies.Append("qat_v2_refresh", result.RefreshToken, new CookieOptions
                                            {
                                                HttpOnly = true,
                                                Secure = true,
                                                SameSite = SameSiteMode.Strict,
                                                Expires = DateTimeOffset.UtcNow.AddHours(2)

                                            });
                                        }
                                    }
                                    else
                                    {
                                        // Valid V2 token and has refresh token, proceed
                                        var jwtToken = tokenHandler.ReadJwtToken(qatV2JWT);

                                        var identity = (ClaimsIdentity)validatedPrincipal.Identity!;
                                        // Add standard claims
                                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? ""));
                                        identity.AddClaim(new Claim(ClaimTypes.Name, jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? ""));
                                        identity.AddClaim(new Claim(ClaimTypes.Email, jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? ""));
                                        identity.AddClaim(new Claim("preferred_username", jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? ""));
                                        identity.AddClaim(new Claim("phone_number", jwtToken.Claims.FirstOrDefault(c => c.Type == "phone_number")?.Value ?? ""));
                                        identity.AddClaim(new Claim("picture", jwtToken.Claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? ""));
                                        identity.AddClaim(new Claim("nickname", jwtToken.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value ?? ""));
                                        identity.AddClaim(new Claim("legacy_uid", jwtToken.Claims.FirstOrDefault(c => c.Type == "legacy_uid")?.Value ?? ""));
                                        identity.AddClaim(new Claim("is_admin", jwtToken.Claims.FirstOrDefault(c => c.Type == "is_admin")?.Value ?? ""));

                                        // Deserialize "user" field for additional claims
                                        var userJson = jwtToken.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                                        if (!string.IsNullOrEmpty(userJson))
                                        {
                                            try
                                            {
                                                var userObj = JsonSerializer.Deserialize<V2User>(userJson);
                                                if (userObj != null)
                                                {
                                                    if (!string.IsNullOrEmpty(userObj.Uid))
                                                        identity.AddClaim(new Claim("uid", userObj.Uid));
                                                    if (!string.IsNullOrEmpty(userObj.QlnextUserId))
                                                        identity.AddClaim(new Claim("qlnext_user_id", userObj.QlnextUserId));
                                                    if (!string.IsNullOrEmpty(userObj.Name))
                                                        identity.AddClaim(new Claim("user_name", userObj.Name));
                                                    if (!string.IsNullOrEmpty(userObj.Alias))
                                                        identity.AddClaim(new Claim("alias", userObj.Alias));
                                                    if (!string.IsNullOrEmpty(userObj.Email))
                                                        identity.AddClaim(new Claim("user_email", userObj.Email));
                                                    if (!string.IsNullOrEmpty(userObj.Phone))
                                                        identity.AddClaim(new Claim("user_phone", userObj.Phone));
                                                    if (!string.IsNullOrEmpty(userObj.Image))
                                                        identity.AddClaim(new Claim("user_image", userObj.Image));
                                                    if (!string.IsNullOrEmpty(userObj.Status))
                                                        identity.AddClaim(new Claim("user_status", userObj.Status));
                                                    if (userObj.Permissions != null)
                                                    {
                                                        foreach (var perm in userObj.Permissions)
                                                            identity.AddClaim(new Claim("permission", perm));
                                                    }
                                                    if (userObj.Roles != null)
                                                    {
                                                        foreach (var role in userObj.Roles)
                                                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("Failed to deserialize V2 user: {0}", ex.Message);
                                            }
                                        }

                                        // Add subscriptions as claims
                                        var subscriptionsJson = jwtToken.Claims.FirstOrDefault(c => c.Type == "subscriptions")?.Value;
                                        if (!string.IsNullOrEmpty(subscriptionsJson))
                                        {
                                            try
                                            {
                                                var subscriptions = JsonSerializer.Deserialize<List<V2Subscription>>(subscriptionsJson);
                                                if (subscriptions != null)
                                                {
                                                    foreach (var sub in subscriptions)
                                                    {
                                                        identity.AddClaim(new Claim("subscription_id", sub.Id));
                                                        identity.AddClaim(new Claim("subscription_display_name", sub.DisplayName));
                                                        identity.AddClaim(new Claim("subscription_product_code", sub.ProductCode));
                                                        identity.AddClaim(new Claim("subscription_product_name", sub.ProductName));
                                                        identity.AddClaim(new Claim("subscription_vertical", sub.Vertical.ToString()));
                                                        identity.AddClaim(new Claim("subscription_subvertical", sub.SubVertical.ToString()));
                                                        identity.AddClaim(new Claim("subscription_start_date", sub.StartDate));
                                                        identity.AddClaim(new Claim("subscription_end_date", sub.EndDate));
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("Failed to deserialize subscriptions: {0}", ex.Message);
                                            }
                                        }

                                        // Add roles and permissions from top-level claims
                                        var roles = jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
                                        foreach (var role in roles)
                                            identity.AddClaim(new Claim(ClaimTypes.Role, role));

                                        var permissions = jwtToken.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
                                        foreach (var perm in permissions)
                                            identity.AddClaim(new Claim("permission", perm));

                                        principal = new ClaimsPrincipal(identity);

                                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", qatV2JWT);
                                    }
                                }
                                else
                                {
                                    // If access token expired, try refresh
                                    if (httpContext.Request.Cookies.TryGetValue("qat_v2_refresh", out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
                                    {
                                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", qatV2JWT);
                                        var refreshResponse = await _httpClient.PostAsync("/api/auth/refresh", null);
                                        if (refreshResponse.IsSuccessStatusCode)
                                        {
                                            var json = await refreshResponse.Content.ReadAsStringAsync();
                                            var result = JsonSerializer.Deserialize<RefreshTokenV2Response>(json);
                                            if (result is not null)
                                            {
                                                // Set new cookies
                                                httpContext.Response.Cookies.Append("qat_v2", result.AccessToken, new CookieOptions
                                                {
                                                    HttpOnly = true,
                                                    Secure = true,
                                                });
                                                httpContext.Response.Cookies.Append("qat_v2_refresh", result.RefreshToken, new CookieOptions
                                                {
                                                    HttpOnly = true,
                                                    Secure = true,
                                                    SameSite = SameSiteMode.Strict,
                                                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                                                });
                                                accessToken = result.AccessToken;
                                            }
                                            // Re-validate with new access token
                                            validatedPrincipal = tokenHandler.ValidateToken(accessToken, validationParameters, out validatedToken);
                                        }
                                        else
                                        {
                                            // Refresh failed, logout
                                            return new AuthenticationState(principal);
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                principal = new ClaimsPrincipal(new ClaimsIdentity());
                            }
                        }
                        else
                        {
                            // No V2 token, proceed with getting a V2 token
                            var configuration = httpContext.RequestServices.GetService<IConfiguration>();
                            var baseAddress = configuration?["ServiceUrlPaths:ContentBOAPI"] ?? "https://qlc-bo-dev.qatarliving.com";
                            var userSyncRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseAddress}/auth/sync");
                            userSyncRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, qatJWT);
                            var userSyncResponse = await _httpClient.SendAsync(userSyncRequest);

                            if (userSyncResponse.IsSuccessStatusCode)
                            {
                                var json = await userSyncResponse.Content.ReadAsStringAsync();
                                var result = JsonSerializer.Deserialize<TokenV2Response>(json, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                                // Set V2 Access Token and V2 Refresh Token
                                httpContext.Response.Cookies.Append("qat_v2", result.AccessToken, new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                });

                                httpContext.Response.Cookies.Append("qat_v2_refresh", result.RefreshToken, new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                                });
                            }
                        }
                    }
                }
                catch
                {
                    principal = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }

            return new AuthenticationState(principal);
        }
    }
}
