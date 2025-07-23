using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalBannerService : IV2BannerService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalBannerService> _logger;
        private readonly IFileStorageBlobService _blobStorage;
        public V2ExternalBannerService(DaprClient dapr, ILogger<V2ExternalBannerService> logger, IFileStorageBlobService blobStorage)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
        }
        public async Task<string> CreateBannerAsync(string uid, V2CreateBannerDto dto, CancellationToken cancellationToken = default)
        {
            string? desktopFileName = null;
            string? mobileFileName = null;
            try
            {
               
                if (!string.IsNullOrWhiteSpace(dto.DesktopImage))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.DesktopImage);
                    if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                        throw new ArgumentException("Desktop Image must be in Jpeg, PNG, Webp, svg or JPG format.");
                    desktopFileName = $"{dto.AltText ?? "desktop"}_{uid}_{Guid.NewGuid()}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, desktopFileName, "imageurl", cancellationToken);
                    dto.DesktopImage = blobUrl;
                }

               
                if (!string.IsNullOrWhiteSpace(dto.MobileImage))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.MobileImage);
                    if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                        throw new ArgumentException("Mobile Image must be in Jpeg, PNG, Webp, svg or JPG format.");
                    mobileFileName = $"{dto.AltText ?? "mobile"}_{uid}_{Guid.NewGuid()}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, mobileFileName, "imageurl", cancellationToken);
                    dto.MobileImage = blobUrl;
                }

                dto.Createdby = uid;

                
                var url = "/api/v2/banner/createbyuserid";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = rawJson;
                    }
                    await CleanupUploadedFiles(new[] { desktopFileName, mobileFileName }, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }

                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Banners created successfully";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(new[] { desktopFileName, mobileFileName }, cancellationToken);
                _logger.LogError(ex, "Error creating banner");
                throw;
            }
        }

        private async Task CleanupUploadedFiles(string?[] files, CancellationToken cancellationToken)
        {
            foreach (var file in files)
            {
                if (!string.IsNullOrWhiteSpace(file))
                    await _blobStorage.DeleteFile(file, "v2banners", cancellationToken);
            }
        }
        public async Task<string> EditBannerAsync(string uid, V2BannerDto dto, CancellationToken cancellationToken = default)
        {
            string? desktopFileName = null;
            string? mobileFileName = null;
            try
            {
                
                if (!string.IsNullOrWhiteSpace(dto.DesktopImage))
                {
                    if (IsBase64Image(dto.DesktopImage))
                    {
                        
                        var (ext, base64Data) = Base64Helper.ParseBase64(dto.DesktopImage);
                        if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                            throw new ArgumentException("Desktop Image must be in JPEG, PNG, WebP, SVG or JPG format.");
                        desktopFileName = $"{dto.AltText ?? "desktop"}_{uid}.{ext}";
                        var blobUrl = await _blobStorage.SaveBase64File(base64Data, desktopFileName, "imageurl", cancellationToken);
                        dto.DesktopImage = blobUrl;
                    }
                    
                }

                
                if (!string.IsNullOrWhiteSpace(dto.MobileImage))
                {
                    if (IsBase64Image(dto.MobileImage))
                    {
                        
                        var (ext, base64Data) = Base64Helper.ParseBase64(dto.MobileImage);
                        if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                            throw new ArgumentException("Mobile Image must be in JPEG, PNG, WebP, SVG or JPG format.");
                        mobileFileName = $"{dto.AltText ?? "mobile"}_{uid}.{ext}";
                        var blobUrl = await _blobStorage.SaveBase64File(base64Data, mobileFileName, "imageurl", cancellationToken);
                        dto.MobileImage = blobUrl;
                    }
                    
                }

                dto.Updatedby = uid;
                dto.UpdatedAt = DateTime.UtcNow; 

                var url = "/api/v2/banner/editbyuserid";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = rawJson;
                    }
                    await CleanupUploadedFiles(new[] { desktopFileName, mobileFileName }, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }

                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(new[] { desktopFileName, mobileFileName }, cancellationToken);
                _logger.LogError(ex, "Error editing banner");
                throw;
            }
        }
        private bool IsBase64Image(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;
            if (input.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                return true;
            try
            {
               
                string base64Data = input;
                if (input.Contains(","))
                {
                    base64Data = input.Split(',')[1];
                }

               
                Convert.FromBase64String(base64Data);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<string> DeleteBannerAsync(string uid, Guid bannerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/banner/deletebyuserid";
                var payload = new
                {
                    BannerId = bannerId,
                    UpdatedBy = uid
                };

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                        errorMessage = problem?.Detail ?? "Unknown error.";
                    }
                    catch
                    {
                        errorMessage = rawJson;
                    }

                    throw new InvalidDataException(errorMessage);
                }

                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner");
                throw;
            }
        }
        public async Task<V2BannerDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/banner/getbyid/{id}";

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation(" Raw JSON from internal service:\n{RawJson}", rawJson);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                        errorMessage = problem?.Detail ?? "Unknown error.";
                    }
                    catch
                    {
                        errorMessage = rawJson;
                    }

                    throw new InvalidDataException(errorMessage);
                }
                return JsonSerializer.Deserialize<V2BannerDto>(
                    rawJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching banner by ID");
                throw;
            }
        }
        public async Task<string> CreateBannerTypeAsync(V2BannerTypeDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/banner/createbannertype";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                    throw new InvalidDataException(problem?.Detail ?? "Unknown error");
                }

                return JsonSerializer.Deserialize<string>(rawJson) ?? "Created";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banner type");
                throw;
            }
        }
        public async Task<List<V2BannerTypeDto>> GetAllBannerTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/banner/getall";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                    throw new InvalidDataException(problem?.Detail ?? "Unknown error");
                }

                return JsonSerializer.Deserialize<List<V2BannerTypeDto>>(rawJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<V2BannerTypeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banner types");
                throw;
            }
        }

        public async Task<List<V2BannerTypeDto>?> GetBannerTypesByFilterAsync(Vertical verticalId,SubVertical? subVerticalId,Guid pageId,CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = new List<string> { $"verticalId={(int)verticalId}" };

                if (subVerticalId.HasValue)
                    queryParams.Add($"subVerticalId={(int)subVerticalId.Value}");

                if (pageId != Guid.Empty)
                    queryParams.Add($"pageId={pageId}");

                var url = $"/api/v2/banner/getbyfilter?" + string.Join("&", queryParams);

                _logger.LogInformation("Constructed URL: {Url}", url);

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation(" Raw JSON from internal: {RawJson}", rawJson);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                        errorMessage = problem?.Detail ?? "Unknown error.";
                    }
                    catch
                    {
                        errorMessage = rawJson;
                    }
                    throw new InvalidDataException("Internal API error: {errorMessage}");
                }

                var result = JsonSerializer.Deserialize<List<V2BannerTypeDto>>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in external GetBannerTypesByFilterAsync");
                throw;
            }
        }


        public async Task<List<V2BannerTypeDto>> GetBannerTypesWithBannersByStatusAsync(Vertical? verticalId,bool? status,CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = new List<string>();
                if (verticalId.HasValue)
                    queryParams.Add($"verticalId={(int)verticalId.Value}");
                if (status.HasValue)
                    queryParams.Add($"status={status.Value.ToString().ToLower()}");

                var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : string.Empty;
                var url = $"/api/v2/banner/getbyverticalandstatus{query}";

                _logger.LogInformation("Calling internal banner hierarchy API: {Url}", url);

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation("Response: {RawJson}", rawJson);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawJson);
                        errorMessage = problem?.Detail ?? "Unknown error.";
                    }
                    catch
                    {
                        errorMessage = rawJson;
                    }
                    throw new InvalidDataException("Internal API error: {errorMessage}");
                }

                var result = JsonSerializer.Deserialize<List<V2BannerTypeDto>>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBannerTypesWithBannersByStatusAsync (external)");
                throw;
            }
        }


        public async Task<string> ReorderAsync(Vertical verticalId,SubVertical? subVerticalId,Guid pageId, List<Guid> banners, CancellationToken cancellationToken = default)
        {
            try
            {
                if (banners == null || banners.Count == 0)
                    throw new ArgumentException("Banners list cannot be empty.");

                var queryParams = new List<string>
        {
            $"verticalId={(int)verticalId}",
            $"pageId={pageId}"
        };

                if (subVerticalId.HasValue)
                    queryParams.Add($"subVerticalId={(int)subVerticalId.Value}");

                var url = $"/api/v2/banner/reorder?" + string.Join("&", queryParams);

                _logger.LogInformation(" Reorder URL: {Url}", url);

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url);

                request.Content = JsonContent.Create(banners);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var resultString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Reorder failed with status {StatusCode}: {Content}", response.StatusCode, resultString);
                    throw new InvalidOperationException("Reorder failed: {resultString}");
                }

                return resultString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Exception in external ReorderBannersAsync");
                throw;
            }
        }

    }
}
