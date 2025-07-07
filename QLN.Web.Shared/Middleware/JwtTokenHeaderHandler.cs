using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace QLN.Web.Shared.Middleware
{
    public class JwtTokenHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtTokenHeaderHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Try to get the cookie value from the incoming request
                if (httpContext.Request.Cookies.TryGetValue("qat", out var jwt) && !string.IsNullOrEmpty(jwt))
                {
                    // set the request headers to add the value of the jwt so we can pass it to the backend
                    request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
