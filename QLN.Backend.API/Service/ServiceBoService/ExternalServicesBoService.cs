

using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Microsoft.AspNetCore.Mvc;
using QLN.Backend.API.Service.Services;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using System.Text.Json;

namespace QLN.Backend.API.Service.ServiceBoService
{
    public class ExternalServicesBoService:IServicesBoService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalServicesBoService> _logger;
        public ExternalServicesBoService(DaprClient dapr, ILogger<ExternalServicesBoService> logger)
        {
            _dapr = dapr;
            _logger = logger;
           
        }
        public async Task<List<ServiceAdSummaryDto>> GetAllServiceBoAds(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/servicebo/getallbo";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.Services.ServiceAppId, url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown error occurred.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    throw new InvalidOperationException(errorMessage);
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var serviceAds = JsonSerializer.Deserialize<List<ServiceAdSummaryDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return serviceAds ?? new List<ServiceAdSummaryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service ads");
                throw;
            }
        }

    }
}
