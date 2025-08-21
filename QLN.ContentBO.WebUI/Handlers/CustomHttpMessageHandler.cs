using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Http.Headers;

namespace QLN.ContentBO.WebUI.Handlers
{
    public class CustomHttpMessageHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public CustomHttpMessageHandler(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.Request.Cookies.TryGetValue("qat", out var jwt) && !string.IsNullOrEmpty(jwt))
                {
                    var configuration = httpContext.RequestServices.GetService<IConfiguration>();
                    var baseAddress = configuration?["ServiceUrlPaths:BOAPIBaseUrl"] ?? "https://qlc-bo-dev.qatarliving.com";
                    var refreshClient = _httpClientFactory.CreateClient("auth");
                    var refreshRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseAddress}/auth/sync");
                    refreshRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt);
                    var refreshResponse = await refreshClient.SendAsync(refreshRequest, cancellationToken);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        // Get the Set-Cookie headers from the refresh response
                        if (refreshResponse.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
                        {
                            foreach (var setCookie in setCookieHeaders)
                            {
                                // Parse cookie name and value
                                var cookieParts = setCookie.Split(';')[0].Split('=', 2);
                                if (cookieParts.Length == 2)
                                {
                                    var cookieName = cookieParts[0].Trim();
                                    var cookieValue = cookieParts[1].Trim();

                                    // Set the cookie in the response (assume HttpOnly, Secure, Strict)
                                    httpContext.Response.Cookies.Append(cookieName, cookieValue, new CookieOptions
                                    {
                                        HttpOnly = true,
                                        Secure = true,
                                        SameSite = SameSiteMode.Strict
                                    });

                                    // Get the new JWT from the cookie
                                    if (cookieName == "QATV2_Access")
                                    {
                                        jwt = cookieValue;
                                        // Set the new JWT in the request header
                                        request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Session expired. Please log in again.");
                    }
                }
                else
                {
                    // If JWT is not present, attempt to refresh the session or handle as needed
                }
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
