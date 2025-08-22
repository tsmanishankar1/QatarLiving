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
                if (httpContext.Request.Cookies.TryGetValue("qat_v2", out var qatJWT) && !string.IsNullOrEmpty(qatJWT))
                {
                    var configuration = httpContext.RequestServices.GetService<IConfiguration>();
                    var baseAddress = configuration?["ServiceUrlPaths:ContentBOAPI"] ?? "https://qlc-bo-dev.qatarliving.com";
                    var refreshClient = _httpClientFactory.CreateClient("auth");
                    var refreshRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseAddress}/auth/sync");
                    refreshRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, qatJWT);
                    var refreshResponse = await refreshClient.SendAsync(refreshRequest, cancellationToken);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        // If the refresh is successful, we can update the JWT in the request headers

                        // Read the response content if needed
                        var json = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
                        var result = JsonSerializer.Deserialize<TokenV2Response>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, result?.AccessToken ?? qatJWT);

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
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

