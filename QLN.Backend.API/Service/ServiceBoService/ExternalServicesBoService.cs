

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
        public async Task<PaginatedResult<ServiceAdSummaryDto>> GetAllServiceBoAds(
    string? sortBy = "CreationDate",
    string? search = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    DateTime? publishedFrom = null,
    DateTime? publishedTo = null,
    int? status = null,
    bool? isFeatured = null,
    bool? isPromoted = null,
    int pageNumber = 1,
    int pageSize = 12,
    CancellationToken cancellationToken = default)
        {
            try
            {
                
                var queryParams = new List<string>
        {
            $"sortBy={Uri.EscapeDataString(sortBy ?? "CreationDate")}",
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (fromDate.HasValue)
                    queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
                if (toDate.HasValue)
                    queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
                if (publishedFrom.HasValue)
                    queryParams.Add($"publishedFrom={Uri.EscapeDataString(publishedFrom.Value.ToString("o"))}");
                if (publishedTo.HasValue)
                    queryParams.Add($"publishedTo={Uri.EscapeDataString(publishedTo.Value.ToString("o"))}");
                if (status.HasValue)
                    queryParams.Add($"status={status}");
                if (isFeatured.HasValue)
                    queryParams.Add($"isFeatured={isFeatured.Value.ToString().ToLowerInvariant()}");
                if (isPromoted.HasValue)
                    queryParams.Add($"isPromoted={isPromoted.Value.ToString().ToLowerInvariant()}");

                var queryString = string.Join("&", queryParams);
                var url = $"/api/servicebo/getallbo?{queryString}";

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
                var paginatedResult = JsonSerializer.Deserialize<PaginatedResult<ServiceAdSummaryDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return paginatedResult ?? new PaginatedResult<ServiceAdSummaryDto>
                {
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = new List<ServiceAdSummaryDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service ads");
                throw;
            }
        }





    }
}
