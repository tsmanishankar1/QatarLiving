using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.Model;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.Services
{
    public class ExternalServicesService : IServices
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalServicesService> _logger;
        public ExternalServicesService(DaprClient dapr, ILogger<ExternalServicesService> logger)
        {
            _dapr = dapr;
            _logger = logger;
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
        public async Task<string> CreateServiceAd(string uid, string userName, ServiceDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/service/createbyuserid?uid={uid}&userName={userName}";
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
                    throw new InvalidDataException(errorMessage);
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
        public async Task<string> UpdateServiceAd(string userId, QLN.Common.Infrastructure.Model.Services dto, CancellationToken cancellationToken = default)
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
        public async Task<QLN.Common.Infrastructure.Model.Services?> GetServiceAdById(long id, CancellationToken cancellationToken = default)
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
        public async Task<ServicesPagedResponse<QLN.Common.Infrastructure.Model.Services>> GetAllServicesWithPagination(BasePaginationQuery? dto, CancellationToken cancellationToken = default)
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
        public async Task<QLN.Common.Infrastructure.Model.Services> PromoteService(PromoteServiceRequest request, string? uid, CancellationToken ct)
        {
            try
            {
                var url = $"/api/service/promotebyuserid?uid={uid}";
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
        public async Task<QLN.Common.Infrastructure.Model.Services> FeatureService(FeatureServiceRequest request, string? uid, CancellationToken ct)
        {
            try
            {
                var url = $"/api/service/featurebyuserid?uid={uid}";
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
                _logger.LogError(ex, "Error featuring service");
                throw;
            }
        }
        public async Task<QLN.Common.Infrastructure.Model.Services> RefreshService(RefreshServiceRequest request, string? uid, CancellationToken ct)
        {
            try
            {
                var url = $"/api/service/refreshbyuserid?uid={uid}";
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
                _logger.LogError(ex, "Error refresh service");
                throw;
            }
        }
        public async Task<QLN.Common.Infrastructure.Model.Services> PublishService(PublishServiceRequest request, string? uid, CancellationToken ct)
        {
            try
            {
                var url = $"/api/service/publishbyuserid?uid={uid}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    ConstantValues.Services.ServiceAppId,
                    url,
                    request
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                var errorJson = await response.Content.ReadAsStringAsync(ct);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var serviceDto = JsonSerializer.Deserialize<QLN.Common.Infrastructure.Model.Services>(errorJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (serviceDto is null)
                            throw new InvalidDataException("Invalid data returned from service.");
                        return serviceDto;

                    case HttpStatusCode.NotFound:
                        var notFound = JsonSerializer.Deserialize<ProblemDetails>(errorJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        throw new KeyNotFoundException(notFound?.Detail ?? "Service not found.");

                    case HttpStatusCode.BadRequest:
                        var badRequest = JsonSerializer.Deserialize<ProblemDetails>(errorJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        throw new InvalidDataException(badRequest?.Detail ?? "Bad request.");

                    case HttpStatusCode.Conflict:
                        var conflict = JsonSerializer.Deserialize<ProblemDetails>(errorJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        throw new ConflictException(conflict?.Detail ?? "Conflict occurred.");

                    default:
                        throw new InvalidDataException($"Service error: {errorJson}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing service");
                throw;
            }
        }
        public async Task<List<QLN.Common.Infrastructure.Model.Services>> ModerateBulkService(BulkModerationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/service/moderatebulkbyuserid";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, cancellationToken);
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
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var moderatedAds = JsonSerializer.Deserialize<List<QLN.Common.Infrastructure.Model.Services>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return moderatedAds ?? new List<QLN.Common.Infrastructure.Model.Services>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating bulk services");
                throw;
            }
        }
        public async Task<SubscriptionBudgetDto> GetSubscriptionBudgetsAsync(
     Guid subscriptionId,
     CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new SubscriptionIdRequest { SubscriptionId = subscriptionId };

                // Call POST endpoint to get budgets by subscriptionId
                var response = await _dapr.InvokeMethodAsync<SubscriptionIdRequest, SubscriptionBudgetDto>(
                    HttpMethod.Post,
                    ConstantValues.Services.ServiceAppId,
                    "/api/service/getbudgets",
                    request,
                    cancellationToken
                );

                return response ?? new SubscriptionBudgetDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription budgets for {SubscriptionId}", subscriptionId);
                throw;
            }
        }
        public async Task<SubscriptionBudgetDto> GetSubscriptionBudgetsAsyncbysubvertical(
    Guid subscriptionId,
    int verticalId,
    CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    SubscriptionId = subscriptionId,
                    VerticalId = verticalId
                };

                // Call POST endpoint to get budgets by subscriptionId and verticalId
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
                Console.WriteLine($"Error fetching subscription budgets for {subscriptionId}: {ex}");
                throw;
            }
        }



    }
}
