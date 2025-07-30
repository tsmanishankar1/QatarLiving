using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;
using static QLN.Common.DTO_s.NotificationDto;

namespace QLN.Backend.API.Service.Services
{
    public class ExternalServicesService : IServices
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalServicesService> _logger;
        private readonly IFileStorageBlobService _blobStorage;
        private readonly ISearchService _searchService;
        public ExternalServicesService(DaprClient dapr, ILogger<ExternalServicesService> logger,
            IFileStorageBlobService blobStorage, ISearchService searchService)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
            _searchService = searchService;
        }
        public async Task<string> CreateCategory(ServicesCategory dto, CancellationToken cancellationToken = default)
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
        public async Task<string> UpdateCategory(ServicesCategory dto, CancellationToken cancellationToken = default)
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
        public async Task<List<ServicesCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<ServicesCategory>>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    "/api/service/getallcategories",
                    cancellationToken
                );
                return response ?? new List<ServicesCategory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving service categories");
                throw;
            }
        }
        public async Task<ServicesCategory?> GetCategoryById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/service/getbycategoryid/{id}";
                return await _dapr.InvokeMethodAsync<object?, ServicesCategory>(
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
        public async Task<string> CreateServiceAd(string userId, ServicesDto dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (dto.PhotoUpload != null && dto.PhotoUpload.Any())
                {
                    for (int i = 0; i < dto.PhotoUpload.Count; i++)
                    {
                        var image = dto.PhotoUpload[i];

                        if (!string.IsNullOrWhiteSpace(image?.Url))
                        {
                            var (ext, base64Data) = Base64Helper.ParseBase64(image.Url!);

                            if (ext is not ("heic" or "png" or "jpg" or "webp"))
                                throw new ArgumentException("Only jpg, png, heic and webp images are allowed.");

                            var imageName = $"{dto.Title}_{userId}_{i}.{ext}";
                            var blobUrl = await _blobStorage.SaveBase64File(base64Data, imageName, "imageurl", cancellationToken);

                            image.FileName = imageName;
                            image.Url = blobUrl;
                        }
                    }
                }
                var url = "/api/service/createbyuserid";
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
                        await CleanupUploadedFiles(FileName, cancellationToken);
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }
                    await response.Content.ReadAsStringAsync(cancellationToken);

                return "Service Ad Created Successfully";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(FileName, cancellationToken);
                _logger.LogError(ex, "Error creating service ad");
                throw;
            }
        }
        private async Task CleanupUploadedFiles(string? file, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(file))
                await _blobStorage.DeleteFile(file, "PhotoUpload", cancellationToken);
        }
        public async Task<string> UpdateServiceAd(string userId, ServicesDto dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (dto.PhotoUpload != null && dto.PhotoUpload.Any())
                {
                    for (int i = 0; i < dto.PhotoUpload.Count; i++)
                    {
                        var image = dto.PhotoUpload[i];

                        if (string.IsNullOrWhiteSpace(image?.Url))
                            throw new ArgumentException("Image URL is required.");

                        if (image.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            image.FileName = Path.GetFileName(new Uri(image.Url).AbsolutePath);
                        }
                        else
                        {
                            var (ext, base64Data) = Base64Helper.ParseBase64(image.Url);

                            if (ext is not ("heic" or "png" or "jpg" or "webp"))
                                throw new ArgumentException("Only heic, jpg, png, and webp images are allowed.");

                            var imageName = $"{dto.Title}_{userId}_{i}.{ext}";
                            var blobUrl = await _blobStorage.SaveBase64File(base64Data, imageName, "imageurl", cancellationToken);

                            image.FileName = imageName;
                            image.Url = blobUrl;
                        }
                    }
                }
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
                        await CleanupUploadedFiles(FileName, cancellationToken);
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }

                return "Service ad updated successfully.";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(FileName, cancellationToken);
                _logger.LogError(ex, "Error updating service ad");
                throw;
            }
        }
        public async Task<List<ServicesDto>> GetAllServiceAds(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<ServicesDto>>(
                    HttpMethod.Get,
                    ConstantValues.Services.ServiceAppId,
                    "/api/service/getall",
                    cancellationToken
                );
                return response ?? new List<ServicesDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving service ads");
                throw;
            }
        }
        public async Task<ServicesDto?> GetServiceAdById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/service/getbyid/{id}";
                return await _dapr.InvokeMethodAsync<object?, ServicesDto>(
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
        public async Task<string> DeleteServiceAdById(string userId, Guid id, CancellationToken cancellationToken = default)
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
                await _searchService.DeleteAsync(ConstantValues.IndexNames.ServicesIndex, id.ToString());
                return "Service ad deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service ad");
                throw;
            }
        }
        public async Task<ServicesPagedResponse<ServicesDto>> GetServicesByStatusWithPagination(ServiceStatusQuery dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/service/getbystatus";
                return await _dapr.InvokeMethodAsync<object?, ServicesPagedResponse<ServicesDto>>(
                    HttpMethod.Post,
                    ConstantValues.Services.ServiceAppId,
                    url,
                    dto,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving paged services by status");
                throw;
            }
        }
        public async Task<ServicesDto> PromoteService(PromoteServiceRequest request, CancellationToken ct)
        {
            try
            {
                var url = "/api/service/promote";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<ServicesDto>(json, new JsonSerializerOptions
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
        public async Task<ServicesDto> FeatureService(FeatureServiceRequest request, CancellationToken ct)
        {
            try
            {
                var url = "/api/service/feature";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<ServicesDto>(json, new JsonSerializerOptions
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
        public async Task<ServicesDto> RefreshService(RefreshServiceRequest request, CancellationToken ct)
        {
            try
            {
                var url = "/api/service/refresh";
                var serviceRequest = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Services.ServiceAppId, url);
                serviceRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<ServicesDto>(json, new JsonSerializerOptions
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
        public async Task<ServicesDto> PublishService(Guid id, CancellationToken ct)
        {
            try
            {
                var url = $"/api/service/publish?id={id}";

                var serviceRequest = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    ConstantValues.Services.ServiceAppId,
                    url
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(serviceRequest, ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var serviceDto = JsonSerializer.Deserialize<ServicesDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (serviceDto is null)
                        throw new InvalidDataException("Invalid data returned from service.");

                    return serviceDto;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException("Service not found.");
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidDataException($"Service error: {errorJson}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing service");
                throw;
            }
        }
        public async Task<List<ServicesDto>> ModerateBulkService(BulkModerationRequest request, CancellationToken cancellationToken = default)
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
                    throw new InvalidDataException(errorMessage);
                }
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var moderatedAds = JsonSerializer.Deserialize<List<ServicesDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (moderatedAds != null && moderatedAds.Any())
                {
                    foreach (var dto in moderatedAds)
                    {
                        //await IndexServiceToAzureSearch(dto, cancellationToken);
                    }
                }
                return moderatedAds ?? new List<ServicesDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating bulk services");
                throw;
            }
        }
    }
}
