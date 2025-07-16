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

        public async Task<string> CreateBannerTypeAsync(V2BannerTypeDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto == null)
                    throw new InvalidDataException("Banner type data cannot be null.");

                if (dto.VerticalId == 0 || dto.SubVerticalId == 0)
                    throw new InvalidDataException("VerticalId and SubVerticalId are required.");

                var url = "api/v2/banner/createbannertype";

                _logger.LogInformation("Invoking Dapr method: AppId = {AppId}, URL = {Url}", ConstantValues.V2Content.ContentServiceAppId, url);

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url
                );

                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                _logger.LogInformation("Dapr response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    if (!string.IsNullOrWhiteSpace(errorContent))
                    {
                        try
                        {
                            if (errorContent.TrimStart().StartsWith("{"))
                            {
                                var problem = JsonSerializer.Deserialize<ProblemDetails>(errorContent);
                                errorMessage = problem?.Detail ?? "Unknown validation error.";
                            }
                            else
                            {
                                errorMessage = errorContent;
                            }
                        }
                        catch
                        {
                            errorMessage = errorContent;
                        }
                    }
                    else
                    {
                        errorMessage = $"Request failed with status code: {response.StatusCode}";
                    }

                    throw new InvalidDataException(errorMessage);
                }

                var result = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Dapr response body: {Result}", result);

                return result;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dapr call to CreateBannerTypeAsync failed. AppId: {AppId}, Path: /api/v2/banner/createbannertype",
                    ConstantValues.V2Content.ContentServiceAppId);
                throw new InvalidDataException($"Failed to create banner type: {ex.Message}", ex);
            }
        }
        public async Task<string> CreateBannerLocationAsync(V2BannerLocationDto dto, CancellationToken cancellationToken = default)
        {
            var request = _dapr.CreateInvokeMethodRequest(
                HttpMethod.Post,
                ConstantValues.V2Content.ContentServiceAppId,
                "/api/v2/banner/createlocation"
            );

            request.Content = new StringContent(
                JsonSerializer.Serialize(dto),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidDataException($"Request failed: {error}");
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        public async Task<string> CreateBannerPageLocationAsync(V2BannerPageLocationDto dto, CancellationToken cancellationToken = default)
        {
            var request = _dapr.CreateInvokeMethodRequest(
                HttpMethod.Post,
                ConstantValues.V2Content.ContentServiceAppId,
                "/api/v2/banner/createpagelocation"
            );

            request.Content = new StringContent(
                JsonSerializer.Serialize(dto),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidDataException($"Request failed: {error}");
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        public async Task<List<V2BannerLocationDto>> GetAllBannerLocationsAsync(CancellationToken cancellationToken = default)
        {
            return await _dapr.InvokeMethodAsync<List<V2BannerLocationDto>>(
                HttpMethod.Get,
                ConstantValues.V2Content.ContentServiceAppId,
                "/api/v2/banner/getlocations",
                cancellationToken);
        }
        public async Task<List<BannerTypeDetailsDto>> GetBannerTypesByVerticalAsync(Vertical vertical, CancellationToken cancellationToken = default)
        {
            var response = await _dapr.InvokeMethodAsync<List<BannerTypeDetailsDto>>(
                HttpMethod.Get,
                ConstantValues.V2Content.ContentServiceAppId,
                $"/api/v2/banner/getbyvertical/{(int)vertical}",
                cancellationToken
            );

            return response ?? new List<BannerTypeDetailsDto>();
        }
        public async Task<List<BannerTypeDetailsDto>> GetBannerTypesBySubVerticalAsync(SubVertical subVertical, CancellationToken cancellationToken = default)
        {
            var response = await _dapr.InvokeMethodAsync<List<BannerTypeDetailsDto>>(
                HttpMethod.Get,
                ConstantValues.V2Content.ContentServiceAppId,
                $"api/v2/banner/getbannertypesbysubvertical/{(int)subVertical}",
                cancellationToken
            );

            return response ?? new();
        }
        public async Task<List<BannerTypeDetailsDto>> GetBannerTypesByPageIdAsync(Guid pageId, CancellationToken cancellationToken = default)
        {
            var response = await _dapr.InvokeMethodAsync<List<BannerTypeDetailsDto>>(
                HttpMethod.Get,
                ConstantValues.V2Content.ContentServiceAppId,
                $"api/v2/banner/getbannertypesbypageid/{pageId}",
                cancellationToken
            );

            return response ?? new();
        }
        public async Task<string> CreateBannerAsync(string userId, V2BannerDto dto, CancellationToken cancellationToken = default)
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

                    desktopFileName = $"{dto.AltText ?? "desktop"}_{userId}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, desktopFileName, "imageurl", cancellationToken);
                    dto.DesktopImage = blobUrl;
                }

                if (!string.IsNullOrWhiteSpace(dto.MobileImage))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.MobileImage);
                    if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                        throw new ArgumentException("Mobile Image must be in Jpeg, PNG, Webp, svg or JPG format.");

                    mobileFileName = $"{dto.AltText ?? "mobile"}_{userId}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, mobileFileName, "imageurl", cancellationToken);
                    dto.MobileImage = blobUrl;
                }

                dto.Createdby = userId;

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
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
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
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.DesktopImage);
                    if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                        throw new ArgumentException("Desktop Image must be in Jpeg, PNG, Webp, svg or JPG format.");

                    desktopFileName = $"{dto.AltText ?? "desktop"}_{uid}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, desktopFileName, "imageurl", cancellationToken);
                    dto.DesktopImage = blobUrl;
                }

                if (!string.IsNullOrWhiteSpace(dto.MobileImage))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.MobileImage);
                    if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                        throw new ArgumentException("Mobile Image must be in Jpeg, PNG, Webp, svg or JPG format.");

                    mobileFileName = $"{dto.AltText ?? "mobile"}_{uid}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, mobileFileName, "imageurl", cancellationToken);
                    dto.MobileImage = blobUrl;
                }

                dto.Updatedby = uid;

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



    }
}
