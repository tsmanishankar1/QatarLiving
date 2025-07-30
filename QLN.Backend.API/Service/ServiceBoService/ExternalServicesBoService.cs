
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
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

        public async Task<PaginatedResult<ServiceAdPaymentSummaryDto>> GetAllServiceAdPaymentSummaries(
      int? pageNumber = 1,
      int? pageSize = 12,
      string? search = null,
      string? sortBy = null,
      CancellationToken cancellationToken = default)
        {
            // Properly encode query params
            var queryParams = new List<string>
    {
        $"pageNumber={pageNumber ?? 1}",
        $"pageSize={pageSize ?? 12}"
    };

            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
            }

            var url = $"/api/servicebo/getalladpayments?{string.Join("&", queryParams)}";

            var request = _dapr.CreateInvokeMethodRequest(
                HttpMethod.Get,
                ConstantValues.Services.ServiceAppId,
                url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                throw new InvalidOperationException(error?.Detail ?? errorJson);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var result = JsonSerializer.Deserialize<PaginatedResult<ServiceAdPaymentSummaryDto>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize response content to PaginatedResult.");
                }

                return result;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException($"Failed to parse response JSON. Raw content: {content}", jsonEx);
            }
        }

        public async Task<PaginatedResult<ServiceP2PAdSummaryDto>> GetAllP2PServiceBoAds(
       string? sortBy = "CreationDate",
       string? search = null,
       DateTime? fromDate = null,
       DateTime? toDate = null,
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
               

                var queryString = string.Join("&", queryParams);
                var url = $"/api/servicebo/getallp2pbo?{queryString}";

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
                var paginatedResult = JsonSerializer.Deserialize<PaginatedResult<ServiceP2PAdSummaryDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return paginatedResult ?? new PaginatedResult<ServiceP2PAdSummaryDto>
                {
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = new List<ServiceP2PAdSummaryDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service ads");
                throw;
            }
        }

        public async Task<PaginatedResult<ServiceSubscriptionAdSummaryDto>> GetAllSubscriptionAdsServiceBo(
                string? sortBy = "CreationDate",
                string? search = null,
                DateTime? fromDate = null,
                DateTime? toDate = null,
                DateTime? publishedFrom = null,
                DateTime? publishedTo = null,
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
              

                var queryString = string.Join("&", queryParams);
                var url = $"/api/servicebo/getallsubscriptionadsbo?{queryString}";

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
                var paginatedResult = JsonSerializer.Deserialize<PaginatedResult<ServiceSubscriptionAdSummaryDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return paginatedResult ?? new PaginatedResult<ServiceSubscriptionAdSummaryDto>
                {
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = new List<ServiceSubscriptionAdSummaryDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service ads");
                throw;
            }
        }
        public async Task<List<CompanyProfileDto>> GetCompaniesByVerticalAsync(
    VerticalType verticalId,
    SubVertical? subVerticalId,
    CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getByVertical?verticalId={(int)verticalId}" +
                          (subVerticalId != null ? $"&subVerticalId={(int)subVerticalId}" : string.Empty);

                return await _dapr.InvokeMethodAsync<List<CompanyProfileDto>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "No companies found for verticalId: {VerticalId}, subVerticalId: {SubVerticalId}", verticalId, subVerticalId);
                return new List<CompanyProfileDto>(); // Or return null if your use case needs it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies for verticalId: {VerticalId}, subVerticalId: {SubVerticalId}", verticalId, subVerticalId);
                throw;
            }
        }


    }

}
