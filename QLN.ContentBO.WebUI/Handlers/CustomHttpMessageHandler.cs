using Microsoft.AspNetCore.Authentication.JwtBearer;
using QLN.ContentBO.WebUI.Models;
using System.Net.Http.Headers;
using System.Text.Json;

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
                // Try to get the cookie value from the incoming request
                if (httpContext.Request.Cookies.TryGetValue("qat_v2", out var jwt) && !string.IsNullOrEmpty(jwt))
                {
                    // set the request headers to add the value of the jwt so we can pass it to the backend
                    request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt);
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
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

