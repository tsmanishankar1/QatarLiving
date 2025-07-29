using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using System.Net;
using System.Text.Json;
using Azure;

namespace QLN.Backend.API.Service.SearchService
{
    public class ExternalSearchService : ISearchService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalSearchService> _logger;
        private readonly string SERVICE_APP_ID = ConstantValues.ServiceAppIds.SearchServiceApp;

        public ExternalSearchService(
            DaprClient dapr,
            ILogger<ExternalSearchService> logger)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Calls SearchService's POST /api/indexes/search?index={indexName}
        /// </summary>
        public async Task<CommonSearchResponse> SearchAsync(string indexName, CommonSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));

            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var methodName = $"/api/indexes/search?index={indexName}";
                var commonResp = await _dapr.InvokeMethodAsync<CommonSearchRequest, CommonSearchResponse>(
                    HttpMethod.Post,
                    appId: SERVICE_APP_ID,
                    methodName: methodName,
                    request
                );

                return commonResp;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "SearchAsync called with null argument.");
                throw;
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex, "Dapr invocation failed in SearchAsync: indexName={IndexName}", indexName);

                if (ex.InnerException is HttpRequestException httpEx)
                {
                    var statusCode = httpEx.StatusCode;
                    var message = ExtractErrorMessage(ex);

                    switch (statusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            if (message.Contains("Invalid filter") ||
                                message.Contains("Invalid date") ||
                                message.Contains("Empty collection") ||
                                message.Contains("Unsupported filter") ||
                                message.Contains("Error building filter") ||
                                message.Contains("Error processing filters"))
                            {
                                throw new ArgumentException($"Invalid filter: {message}", nameof(request));
                            }
                            else if (message.Contains("PageNumber") ||
                                     message.Contains("PageSize") ||
                                     message.Contains("OrderBy"))
                            {
                                throw new ArgumentException($"Invalid parameter: {message}", nameof(request));
                            }
                            else if (message.Contains("Unsupported Index") || message.Contains("Unknown index"))
                            {
                                throw new NotSupportedException($"Unsupported index: {message}");
                            }
                            else
                            {
                                throw new ArgumentException($"Bad request: {message}", nameof(request));
                            }

                        case HttpStatusCode.NotFound:
                            throw new KeyNotFoundException($"Resource not found: {message}");

                        case HttpStatusCode.InternalServerError:
                            throw new InvalidOperationException($"Search service error: {message}");

                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                            throw new RequestFailedException((int)statusCode, $"Search service unavailable: {message}");

                        default:
                            throw new InvalidOperationException($"Search service returned {statusCode}: {message}");
                    }
                }

                throw new InvalidOperationException($"Dapr communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchAsync: indexName={IndexName}", indexName);
                throw new InvalidOperationException($"Unexpected error in SearchAsync: {ex.Message}", ex);
            }
        }

        public async Task<CommonSearchResponse> GetAllAsync(string indexName, CommonSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));

            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var methodName = $"/api/indexes/getAll?index={indexName}";
                var commonResp = await _dapr.InvokeMethodAsync<CommonSearchRequest, CommonSearchResponse>(
                    HttpMethod.Post,
                    appId: SERVICE_APP_ID,
                    methodName: methodName,
                    request
                );

                return commonResp;
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex, "Dapr invocation failed in GetAllAsync: indexName={IndexName}", indexName);

                if (ex.InnerException is HttpRequestException httpEx)
                {
                    var statusCode = httpEx.StatusCode;
                    var message = ExtractErrorMessage(ex);

                    switch (statusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            if (message.Contains("Invalid filter") ||
                                message.Contains("Invalid date") ||
                                message.Contains("Empty collection") ||
                                message.Contains("Unsupported filter") ||
                                message.Contains("Error building filter") ||
                                message.Contains("Error processing filters"))
                            {
                                throw new ArgumentException($"Invalid filter: {message}", nameof(request));
                            }
                            else if (message.Contains("PageNumber") ||
                                     message.Contains("PageSize") ||
                                     message.Contains("OrderBy"))
                            {
                                throw new ArgumentException($"Invalid parameter: {message}", nameof(request));
                            }
                            else if (message.Contains("Unsupported Index") || message.Contains("Unknown index"))
                            {
                                throw new NotSupportedException($"Unsupported index: {message}");
                            }
                            else
                            {
                                throw new ArgumentException($"Bad request: {message}", nameof(request));
                            }

                        case HttpStatusCode.NotFound:
                            throw new KeyNotFoundException($"Resource not found: {message}");

                        case HttpStatusCode.InternalServerError:
                            throw new InvalidOperationException($"GetAll service error: {message}");

                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                            throw new RequestFailedException((int)statusCode, $"GetAll service unavailable: {message}");

                        default:
                            throw new InvalidOperationException($"GetAll service returned {statusCode}: {message}");
                    }
                }

                throw new InvalidOperationException($"Dapr communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllAsync: indexName={IndexName}", indexName);
                throw new InvalidOperationException($"Unexpected error in GetAllAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calls SearchService's POST /api/indexes/upload
        /// </summary>
        public async Task<string> UploadAsync(CommonIndexRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var methodName = "/api/indexes/upload";

                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    appId: SERVICE_APP_ID,
                    methodName: methodName,
                    request
                );

                return "Document indexed successfully";
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "UploadAsync called with null request.");
                throw;
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex, "Dapr invocation failed in UploadAsync: {@Request}", request);

                if (ex.InnerException is HttpRequestException httpEx)
                {
                    var statusCode = httpEx.StatusCode;
                    var message = ExtractErrorMessage(ex);

                    switch (statusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            throw new ArgumentException($"Invalid upload request: {message}", nameof(request));

                        case HttpStatusCode.InternalServerError:
                            throw new InvalidOperationException($"Upload service error: {message}");

                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                            throw new RequestFailedException((int)statusCode, $"Upload service unavailable: {message}");

                        default:
                            throw new InvalidOperationException($"Upload service returned {statusCode}: {message}");
                    }
                }

                throw new InvalidOperationException($"Dapr communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UploadAsync: {@Request}", request);
                throw new InvalidOperationException($"Unexpected error in UploadAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calls SearchService's GET /api/indexes/{indexName}/{key}
        /// </summary>
        public async Task<T?> GetByIdAsync<T>(string indexName, string key)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
                var methodName = $"/api/indexes/{indexName}/{key}";
                var doc = await _dapr.InvokeMethodAsync<T?>(
                    HttpMethod.Get,
                    appId: SERVICE_APP_ID,
                    methodName: methodName
                );

                return doc;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "GetByIdAsync called with null argument: {Param}", ex.ParamName);
                throw;
            }
            catch (DaprException ex)
            {
                if (ex.InnerException is HttpRequestException httpEx)
                {
                    var statusCode = httpEx.StatusCode;
                    var message = ExtractErrorMessage(ex);

                    switch (statusCode)
                    {
                        case HttpStatusCode.NotFound:
                            _logger.LogWarning("Remote returned 404 for {IndexName}/{Key}", indexName, key);
                            return default(T); // Return null/default instead of throwing for GetById

                        case HttpStatusCode.BadRequest:
                            throw new ArgumentException($"Invalid GetById request: {message}");

                        case HttpStatusCode.InternalServerError:
                            throw new InvalidOperationException($"GetById service error: {message}");

                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                            throw new RequestFailedException((int)statusCode, $"GetById service unavailable: {message}");

                        default:
                            throw new InvalidOperationException($"GetById service returned {statusCode}: {message}");
                    }
                }

                _logger.LogError(ex, "Dapr invocation failed in GetByIdAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw new InvalidOperationException($"Dapr communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByIdAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw new InvalidOperationException($"Unexpected error in GetByIdAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calls SearchService's DELETE /api/indexes/{indexName}/{key}
        /// </summary>
        public async Task DeleteAsync(string indexName, string key)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
                var methodName = $"/api/indexes/{indexName}/{key}";
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    appId: SERVICE_APP_ID,
                    methodName: methodName
                );
                _logger.LogInformation("Deleted document '{Key}' from indexName '{IndexName}'", key, indexName);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "DeleteAsync called with null argument: {Param}", ex.ParamName);
                throw;
            }
            catch (DaprException ex)
            {
                if (ex.InnerException is HttpRequestException httpEx)
                {
                    var statusCode = httpEx.StatusCode;
                    var message = ExtractErrorMessage(ex);

                    switch (statusCode)
                    {
                        case HttpStatusCode.NotFound:
                            throw new KeyNotFoundException($"Document '{key}' not found in '{indexName}' for deletion.");

                        case HttpStatusCode.BadRequest:
                            throw new ArgumentException($"Invalid delete request: {message}");

                        case HttpStatusCode.InternalServerError:
                            throw new InvalidOperationException($"Delete service error: {message}");

                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                            throw new RequestFailedException((int)statusCode, $"Delete service unavailable: {message}");

                        default:
                            throw new InvalidOperationException($"Delete service returned {statusCode}: {message}");
                    }
                }

                _logger.LogError(ex, "Dapr invocation failed in DeleteAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw new InvalidOperationException($"Dapr communication failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw new InvalidOperationException($"Unexpected error in DeleteAsync: {ex.Message}", ex);
            }
        }

        /// <summary>  
        /// Calls SearchService's GET /api/indexes/{indexName}/{key}/details?similarPageSize={n}  
        /// </summary>  
        public async Task<GetWithSimilarResponse<T>> GetByIdWithSimilarAsync<T>(
            string indexName,
            string key,
            int similarPageSize = 10
        ) where T : class
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("IndexName is required.", nameof(indexName));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
                var methodName = $"/api/indexes/{indexName}/{key}/details?similarPageSize={similarPageSize}";
                return await _dapr.InvokeMethodAsync<GetWithSimilarResponse<T>>(
                    HttpMethod.Get,
                    appId: SERVICE_APP_ID,
                    methodName: methodName
                );
            }
            catch (DaprException ex)
            {
                if (ex.InnerException is HttpRequestException httpEx)
                {
                    var statusCode = httpEx.StatusCode;
                    var message = ExtractErrorMessage(ex);

                    switch (statusCode)
                    {
                        case HttpStatusCode.NotFound:
                            _logger.LogWarning("Remote returned 404 for {IndexName}/{Key}", indexName, key);
                            throw new KeyNotFoundException($"Document '{key}' not found in '{indexName}'.");

                        case HttpStatusCode.BadRequest:
                            if (message.Contains("SimilarPageSize"))
                            {
                                throw new ArgumentException($"Invalid similarPageSize parameter: {message}", nameof(similarPageSize));
                            }
                            throw new ArgumentException($"Invalid GetByIdWithSimilar request: {message}");

                        case HttpStatusCode.InternalServerError:
                            throw new InvalidOperationException($"GetByIdWithSimilar service error: {message}");

                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                        case HttpStatusCode.GatewayTimeout:
                            throw new RequestFailedException((int)statusCode, $"GetByIdWithSimilar service unavailable: {message}");

                        default:
                            throw new InvalidOperationException($"GetByIdWithSimilar service returned {statusCode}: {message}");
                    }
                }

                _logger.LogError(ex, "Dapr invocation failed in GetByIdWithSimilarAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw new InvalidOperationException($"Dapr communication failed: {ex.Message}", ex);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "GetByIdWithSimilarAsync called with null argument");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByIdWithSimilarAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw new InvalidOperationException($"Unexpected error in GetByIdWithSimilarAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extract error message from DaprException, attempting to get the most meaningful error message
        /// </summary>
        private string ExtractErrorMessage(DaprException daprEx)
        {
            try
            {
                if (daprEx.InnerException is HttpRequestException httpEx)
                {
                    var errorMessage = daprEx.Message;

                    if (daprEx.Data.Contains("response"))
                    {
                        var responseData = daprEx.Data["response"]?.ToString();
                        if (!string.IsNullOrEmpty(responseData))
                        {
                            try
                            {
                                using var jsonDoc = JsonDocument.Parse(responseData);
                                if (jsonDoc.RootElement.TryGetProperty("detail", out var detailElement))
                                {
                                    return detailElement.GetString() ?? errorMessage;
                                }
                                if (jsonDoc.RootElement.TryGetProperty("title", out var titleElement))
                                {
                                    return titleElement.GetString() ?? errorMessage;
                                }
                            }
                            catch (JsonException)
                            {
                            }
                        }
                    }

                    return errorMessage;
                }

                return daprEx.Message;
            }
            catch
            {
                return daprEx.Message;
            }
        }
    }
}