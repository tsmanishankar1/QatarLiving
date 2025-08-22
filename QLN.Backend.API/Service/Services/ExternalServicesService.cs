using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.Services
{
    public class ExternalServicesService : IServices
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalServicesService> _logger;
        private readonly IV2SubscriptionService _v2SubscriptionService;
        public ExternalServicesService(DaprClient dapr, ILogger<ExternalServicesService> logger,IV2SubscriptionService v2SubscriptionService)
        {
            _dapr = dapr;
            _logger = logger;
            _v2SubscriptionService=v2SubscriptionService;
        }
        public async Task<string> CreateCategory(CategoryDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var url = "/api/service/createcategory";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<string> UpdateCategory(CategoryDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/service/updatecategory";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.Services.ServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }

                    throw new InvalidDataException(errorMessage);
                }

                return "Category updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                throw;
            }
        }
        public async Task<List<CategoryDto>> GetAllCategories(string? vertical, string? subVertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new QueryString(string.Empty);

                if (!string.IsNullOrWhiteSpace(vertical))
                    query = query.Add("vertical", vertical);

                if (!string.IsNullOrWhiteSpace(subVertical))
                    query = query.Add("subVertical", subVertical);

                var uri = $"/api/service/getallcategories{query}";

                var response = await _dapr.InvokeMethodAsync<List<CategoryDto>>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    uri,
                    cancellationToken
                );

                return response ?? new List<CategoryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving service categories");
                throw;
            }
        }
        public async Task<CategoryDto?> GetCategoryById(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/service/getbycategoryid/{id}";
                return await _dapr.InvokeMethodAsync<object?, CategoryDto>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    url,
                    null,
                    cancellationToken
                );
            }
            catch (InvocationException ex) when (ex.InnerException is HttpRequestException httpEx &&
                                          httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Service ad not found for ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving service category by ID");
                throw;
            }
        }
        public async Task<string> CreateServiceAd(string uid, string userName, string subscriptionId, ServiceDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.AdType == ServiceAdType.Subscription)
                {
                    var subscription = new V2SubscriptionResponseDto();
                    if(subscriptionId != null)
                        subscription = await _v2SubscriptionService.GetSubscriptionByIdAsync(Guid.Parse(subscriptionId), cancellationToken);

                    if (subscription == null)
                        throw new InvalidDataException("Subscription not found.");

                    if (subscription.StatusId != SubscriptionStatus.Active || subscription.EndDate <= DateTime.UtcNow)
                        throw new InvalidDataException("Subscription is inactive or expired.");

                    var canUse = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId),
                        ActionTypes.Publish, 
                        1,
                        cancellationToken
                    );

                    if (!canUse)
                    {
                        throw new InvalidDataException("Insufficient subscription quota to create this ad.");
                    }
                }

                var url = $"/api/service/createbyuserid?uid={uid}&userName={userName}&subscriptionId={subscriptionId}";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    if (response.StatusCode == HttpStatusCode.Conflict)
                    {
                        throw new ConflictException(errorMessage);
                    }
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new InvalidDataException(errorMessage); 
                    }
                    throw new Exception(errorMessage);
                }
                if (Guid.Parse(subscriptionId) != Guid.Empty)
                {
                    var success = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId),
                        ActionTypes.Publish,
                        1,
                        cancellationToken
                    );

                    if (!success)
                    {
                        _logger.LogWarning(
                            "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                            subscriptionId
                        );
                    }
                }
                await response.Content.ReadAsStringAsync(cancellationToken);

                return "Service Ad Created Successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service ad");
                throw;
            }
        }
        public async Task<string> UpdateServiceAd(string userId, Common.Infrastructure.Model.Services dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/service/updatebyuserid";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.Services.ServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    if (response.StatusCode == HttpStatusCode.Conflict)
                    {
                        throw new ConflictException(errorMessage);
                    }
                    throw new InvalidDataException(errorMessage);
                }
                return "Service ad updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service ad");
                throw;
            }
        }
        public async Task<Common.Infrastructure.Model.Services?> GetServiceAdById(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/service/getbyid/{id}";
                return await _dapr.InvokeMethodAsync<object?, QLN.Common.Infrastructure.Model.Services>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    url,
                    null,
                    cancellationToken
                );
            }
            catch (InvocationException ex) when (ex.InnerException is HttpRequestException httpEx &&
                                          httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Service ad not found for ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving service ad by ID");
                throw;
            }
        }
        public async Task<Common.Infrastructure.Model.Services?> GetServiceAdBySlug(string? slug, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/service/getbyslug/{Uri.EscapeDataString(slug)}";
                return await _dapr.InvokeMethodAsync<object?, QLN.Common.Infrastructure.Model.Services>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    url,
                    null,
                    cancellationToken
                );
            }
            catch (InvocationException ex) when (ex.InnerException is HttpRequestException httpEx &&
                                          httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Service ad not found for slug}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving service ad by slug");
                throw;
            }
        }

        public async Task<string> DeleteServiceAdById(string userId, long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = new DeleteServiceRequest
                {
                    Id = id,
                    UpdatedBy = userId
                };

                var url = "/api/service/deletebyuserid";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }
                return "Service ad deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service ad");
                throw;
            }
        }
        public async Task<ServicesPagedResponse<Common.Infrastructure.Model.Services>> GetAllServicesWithPagination(BasePaginationQuery? dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/service/getallwithpagination";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    throw new InvalidDataException(problem?.Detail ?? "Unknown error occurred.");
                }
                var result = await response.Content.ReadFromJsonAsync<ServicesPagedResponse<QLN.Common.Infrastructure.Model.Services>>(cancellationToken: cancellationToken);
                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving paged services");
                throw;
            }
        }
        public async Task<Common.Infrastructure.Model.Services> PromoteService(PromoteServiceRequest request, string? uid, string? subscriptionId, CancellationToken ct)
        {
            try
            {
                var quotaAction = request.IsPromoted ? ActionTypes.Promote : ActionTypes.UnPromote;

                if (quotaAction == ActionTypes.Promote)
                {
                    var canUse = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId),
                        quotaAction,
                        1,
                        ct
                    );

                    if (!canUse)
                    {
                        _logger.LogWarning(
                            "Subscription {SubscriptionId} has insufficient quota for {QuotaAction}.",
                            subscriptionId, 
                            quotaAction
                        );
                        throw new InvalidDataException($"Insufficient subscription quota for {quotaAction.ToLower()}.");
                    }
                }

                var url = $"/api/service/promotebyuserid?uid={uid}&subscriptionId={subscriptionId}";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    ConstantValues.Services.ServiceAppId,
                    url
                );
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<Common.Infrastructure.Model.Services>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (serviceDto is null)
                        throw new InvalidDataException("Invalid data returned from service.");

                    if (serviceDto.SubscriptionId != Guid.Empty)
                    {
                        var success = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                            Guid.Parse(subscriptionId),
                            quotaAction,
                            1,
                            ct
                        );

                        if (!success)
                        {
                            _logger.LogWarning(
                                "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                                serviceDto.SubscriptionId
                            );
                        }
                    }

                    return serviceDto;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException("Service not found");
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidDataException(errorJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting service");
                throw;
            }
        }
        public async Task<Common.Infrastructure.Model.Services> FeatureService(FeatureServiceRequest request, string? uid, string? subscriptionId, CancellationToken ct = default)
        {
            try
            {
                var quotaAction = request.IsFeature ? ActionTypes.Feature : ActionTypes.UnFeature;

                if (quotaAction == ActionTypes.Feature)
                {
                    var canUse = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId),
                        quotaAction,
                        1,
                        ct
                    );

                    if (!canUse)
                    {
                        _logger.LogWarning(
                            "Subscription {SubscriptionId} has insufficient quota for {QuotaAction}.",
                            subscriptionId,
                            quotaAction
                        );
                        throw new InvalidDataException($"Insufficient subscription quota for {quotaAction.ToLower()}.");
                    }
                }
                var url = $"/api/service/featurebyuserid?uid={uid}&subscriptionId={subscriptionId}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    ConstantValues.Services.ServiceAppId,
                    url
                );
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<Common.Infrastructure.Model.Services>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (serviceDto is null)
                        throw new InvalidDataException("Invalid data returned from service.");

                    if (serviceDto.SubscriptionId != Guid.Empty)
                    {
                        var success = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                            Guid.Parse(subscriptionId),
                            quotaAction,
                            1,
                            ct
                        );

                        if (!success)
                        {
                            _logger.LogWarning(
                                "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                                serviceDto.SubscriptionId
                            );
                        }
                    }

                    return serviceDto;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException("Service not found");
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidDataException(errorJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting service");
                throw;
            }
        }
        public async Task<Common.Infrastructure.Model.Services> RefreshService(RefreshServiceRequest request, string? uid, string? subscriptionId, CancellationToken ct)
        {
            try
            {
                var quotaAction = request.IsRefreshed ? ActionTypes.Refresh : null;

                if (quotaAction == ActionTypes.Refresh)
                {
                    var canUse = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId),
                        quotaAction,
                        1,
                        ct
                    );

                    if (!canUse)
                    {
                        _logger.LogWarning(
                            "Subscription {SubscriptionId} has insufficient quota for {QuotaAction}.",
                            subscriptionId, 
                            quotaAction
                        );
                        throw new InvalidDataException($"Insufficient subscription quota for {quotaAction.ToLower()}.");
                    }
                }

                var url = $"/api/service/refreshbyuserid?uid={uid}&subscriptionId={subscriptionId}";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<QLN.Common.Infrastructure.Model.Services>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (serviceDto is null)
                        throw new InvalidDataException("Invalid data returned from service.");

                    if (serviceDto.SubscriptionId.HasValue && serviceDto.SubscriptionId.Value != Guid.Empty)
                    {
                        var success = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                            Guid.Parse(subscriptionId),
                            quotaAction,
                            1,
                            ct
                        );

                        if (!success)
                        {
                            _logger.LogWarning(
                                "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                                serviceDto.SubscriptionId
                            );
                        }
                    }

                    return serviceDto;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException("Service not found");
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidDataException(errorJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing service");
                throw;
            }
        }
        public async Task<Common.Infrastructure.Model.Services> PublishService(PublishServiceRequest request, string? uid, string? subscriptionId, CancellationToken ct)
        {
            try
            {
                var quotaActionPublish = request.Status == ServiceStatus.Published
                    ? ActionTypes.Publish
                    : ActionTypes.UnPublish;

                var quotaActionPromote = request.Status == ServiceStatus.Published
                    ? ActionTypes.Promote
                    : ActionTypes.UnPromote;

                var quotaActionFeature = request.Status == ServiceStatus.Published
                    ? ActionTypes.Feature
                    : ActionTypes.UnFeature;

                if (request.Status == ServiceStatus.Published)
                {
                    var canPublish = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId), quotaActionPublish, 1, ct);

                    var canPromote = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId), quotaActionPromote, 1, ct);

                    var canFeature = await _v2SubscriptionService.ValidateSubscriptionUsageAsync(
                        Guid.Parse(subscriptionId), quotaActionFeature, 1, ct);

                    if (!canPublish || !canPromote || !canFeature)
                    {
                        _logger.LogWarning(
                            "Subscription {SubscriptionId} insufficient quota. Publish={Publish}, Promote={Promote}, Feature={Feature}",
                            subscriptionId, canPublish, canPromote, canFeature);

                        throw new InvalidDataException("Insufficient subscription quota.");
                    }
                }
                var url = $"/api/service/publishbyuserid?uid={uid}&subscriptionId={subscriptionId}";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<QLN.Common.Infrastructure.Model.Services>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (serviceDto is null)
                        throw new InvalidDataException("Invalid data returned from service.");

                    if (serviceDto.SubscriptionId != Guid.Empty)
                    {
                        var success = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                            Guid.Parse(subscriptionId),
                            quotaActionPublish,
                            1,
                            ct
                        );
                        var success1 = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                            Guid.Parse(subscriptionId),
                            quotaActionPromote,
                            1,
                            ct
                        );
                        var success2 = await _v2SubscriptionService.RecordSubscriptionUsageAsync(
                            Guid.Parse(subscriptionId),
                            quotaActionFeature,
                            1,
                            ct
                        );
                        if (!success)
                        {
                            _logger.LogWarning(
                                "Failed to record subscription usage for SubscriptionId {SubscriptionId}",
                                serviceDto.SubscriptionId
                            );
                        }
                    }

                    return serviceDto;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException("Service not found");
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidDataException(errorJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing service");
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
                    => new[] { ActionTypes.Publish, ActionTypes.Promote, ActionTypes.Feature },

                BulkModerationAction.Unpublish
                    => new[] { ActionTypes.UnPublish, ActionTypes.UnPromote, ActionTypes.UnFeature },

                _ => Array.Empty<string>()
            };
        }

        public async Task<BulkAdActionResponseitems> ModerateBulkService(BulkModerationRequest request, string? userId, string subscriptionGuid, DateTime? expiryDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var failedIds = new List<long>();
                var succeededIds = new List<long>();

                var ads = await Task.WhenAll(
                    request.AdIds.Select(id => GetServiceAdById(id, cancellationToken))
                );

                var subscriptionId = Guid.Parse(subscriptionGuid);

                var groupedActions = ads
                    .Where(a => a != null && a.SubscriptionId.HasValue && a.SubscriptionId.Value != Guid.Empty)
                    .GroupBy(a => new { ActionType = request.Action })
                    .ToList();

                foreach (var group in groupedActions)
                {
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
                        var endDateParam = expiryDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "";
                        var url = $"/api/service/moderatebulkbyuserid?userId={userId}&subscriptionId={subscriptionId}&endDate={Uri.EscapeDataString(endDateParam)}";

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
        public async Task<SubscriptionBudgetDto> GetSubscriptionBudgetsAsyncBySubVertical(Guid subscriptionIdFromToken, Vertical verticalId, SubVertical? subverticalId, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    SubscriptionId = subscriptionIdFromToken,
                    VerticalId = verticalId,
                    SubVerticalId = subverticalId 
                };

                var response = await _dapr.InvokeMethodAsync<object, SubscriptionBudgetDto>(
                    HttpMethod.Post,
                    ConstantValues.Services.ServiceAppId,
                    "/api/service/getbudgetsbysubvertical",
                    request,
                    cancellationToken
                );

                return response ?? new SubscriptionBudgetDto();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching subscription budgets for : {ex}");
                throw;
            }
        }
        public async Task<List<CategoryAdCountDto>> GetCategoryAdCount(CancellationToken ct = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<CategoryAdCountDto>>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    "/api/service/getcategoryadcount",
                    cancellationToken: ct
                );
                return response ?? new List<CategoryAdCountDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category ad counts");
                throw;
            }
        }

        public Task<string> MigrateServiceAd(Common.Infrastructure.Model.Services dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
