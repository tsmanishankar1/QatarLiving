
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.ServiceBoService
{
    public class ExternalServicesBoService:IServicesBoService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalServicesBoService> _logger;
        private readonly IV2SubscriptionService _v2SubscriptionService;
        private readonly IServices _services;
        public ExternalServicesBoService(DaprClient dapr, ILogger<ExternalServicesBoService> logger, IV2SubscriptionService v2SubscriptionService, IServices services)
        {
            _dapr = dapr;
            _logger = logger;
            _v2SubscriptionService = v2SubscriptionService;
            _services = services;
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
      DateTime? startDate = null,
      DateTime? endDate = null,
      string? subscriptionType = null,
      CancellationToken cancellationToken = default)
        {
            var queryParams = new List<string>
    {
        $"pageNumber={pageNumber ?? 1}",
        $"pageSize={pageSize ?? 12}"
    };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

            if (startDate.HasValue)
                queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("yyyy-MM-dd"))}");

            if (endDate.HasValue)
                queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("yyyy-MM-dd"))}");

            if (!string.IsNullOrWhiteSpace(subscriptionType))
                queryParams.Add($"subscriptionType={Uri.EscapeDataString(subscriptionType)}");

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
                    throw new InvalidOperationException("Failed to deserialize response content to PaginatedResult.");

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
        private static IEnumerable<string> GetQuotaActions(BulkModerationAction action)
        {
            return action switch
            {
                BulkModerationAction.Promote => new[] { ActionTypes.Promote },
                BulkModerationAction.UnPromote => new[] { ActionTypes.UnPromote },
                BulkModerationAction.Feature => new[] { ActionTypes.Feature },
                BulkModerationAction.UnFeature => new[] { ActionTypes.UnFeature },

                BulkModerationAction.Publish or BulkModerationAction.Approve
                    => new[] { ActionTypes.Publish },

                BulkModerationAction.Unpublish
                    => new[] { ActionTypes.UnPublish },

                _ => Array.Empty<string>()
            };
        }

        public async Task<BulkAdActionResponseitems> ModerateBulkService(BulkModerationRequest request, string? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var failedIds = new List<long>();
                var succeededIds = new List<long>();

                var ads = await Task.WhenAll(
                    request.AdIds.Select(id => _services.GetServiceAdById(id, cancellationToken))
                );

                var groupedActions = ads
                    .Where(a => a != null && a.SubscriptionId.HasValue && a.SubscriptionId.Value != Guid.Empty)
                    .GroupBy(a => new { a.SubscriptionId, ActionType = request.Action })
                    .ToList();

                foreach (var group in groupedActions)
                {
                    var subscriptionId = group.Key.SubscriptionId!.Value;
                    var quotaActions = GetQuotaActions(group.Key.ActionType);
                    var usageCount = group.Count();
                    var requiresQuota = quotaActions.Any();

                    if (requiresQuota)
                    {
                        var canUseAll = true;

                        foreach (var quotaAction in quotaActions)
                        {
                            var canUse = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                                subscriptionId,
                                quotaAction,
                                usageCount,
                                cancellationToken
                            );

                            if (!canUse)
                            {
                                canUseAll = false;
                                break;
                            }
                        }

                        if (!canUseAll)
                        {
                            failedIds.AddRange(group.Select(x => x.Id));
                            continue;
                        }

                        foreach (var quotaAction in quotaActions)
                        {
                            var reserved = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                                subscriptionId,
                                quotaAction,
                                usageCount,
                                cancellationToken
                            );

                            if (!reserved)
                            {
                                failedIds.AddRange(group.Select(x => x.Id));
                                continue;
                            }
                        }
                    }

                    try
                    {
                        var url = $"/api/servicebo/bobulkactions?userId={userId}";

                        var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                        serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                        _logger.LogInformation($"Calling internal service: {url} with payload: {JsonSerializer.Serialize(request)}");

                        var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);
                        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                        _logger.LogInformation($"Response from internal service - Status: {response.StatusCode}, Content: {responseContent}");

                        if (!response.IsSuccessStatusCode)
                        {
                            foreach (var quotaAction in quotaActions)
                            {
                                await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                                    subscriptionId,
                                    quotaAction,
                                    -usageCount,
                                    cancellationToken
                                );
                            }

                            throw new InvalidOperationException($"Bulk action failed for {group.Key.ActionType}. Reason: {responseContent}");
                        }

                        succeededIds.AddRange(group.Select(x => x.Id));
                    }
                    catch
                    {
                        if (requiresQuota)
                        {
                            foreach (var quotaAction in quotaActions)
                            {
                                await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                                    subscriptionId,
                                    quotaAction,
                                    -usageCount,
                                    cancellationToken
                                );
                            }
                        }

                        failedIds.AddRange(group.Select(x => x.Id));
                        continue;
                    }
                }

                return new BulkAdActionResponseitems
                {
                    Failed = new ResultGroup
                    {
                        Count = failedIds.Count,
                        Ids = failedIds,
                        Reason = failedIds.Any() ? "Failed actions" : null
                    },
                    Succeeded = new ResultGroup
                    {
                        Count = succeededIds.Count,
                        Ids = succeededIds,
                        Reason = "Success"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating bulk services");
                throw;
            }
        }
    }

}
