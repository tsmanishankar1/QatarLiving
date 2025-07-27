using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.Payments
{
    internal class D365Service : ID365Service
    {
        private readonly ILogger<D365Service> _logger;
        private readonly HttpClient _httpClient;
        private readonly D365Config _d365Config;

        public D365Service(
            ILogger<D365Service> logger,
            HttpClient httpClient,
            D365Config d365Config
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _d365Config = d365Config;

            // Acquire Bearer token using MSAL
            var authority = $"https://login.microsoftonline.com/{_d365Config.TenantId}";
            var app = ConfidentialClientApplicationBuilder.Create(_d365Config.ClientId)
                .WithClientSecret(_d365Config.ClientSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            var scopes = new[] { $"{_d365Config.D365Url}/.default" };
            var tokenResult = app.AcquireTokenForClient(scopes).ExecuteAsync().GetAwaiter().GetResult();

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
        }


        public Task<string> HandleD365OrderAsync(D365Order order)
        {
            _logger.LogInformation("Handling D365 order with ID: {OrderId}", order.OrderId);

            throw new NotImplementedException();
        }
    }
}
