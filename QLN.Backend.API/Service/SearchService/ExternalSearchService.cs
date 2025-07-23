using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using System.Net;
using System.Text.Json;

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
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchAsync: indexName={IndexName}", indexName);
                throw;
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
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllAsync: indexName={IndexName}", indexName);
                throw;
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

                // Use HttpRequestMessage to handle the response properly
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
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UploadAsync: {@Request}", request);
                throw;
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
                if (ex.InnerException is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Remote returned 404 for {IndexName}/{Key}", indexName, key);
                    throw new KeyNotFoundException($"Document '{key}' not found in '{indexName}'.");
                }
                _logger.LogError(ex, "Dapr invocation failed in GetByIdAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByIdAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw;
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
                _logger.LogError(ex, "Dapr invocation failed in DeleteAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw;
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
                if (ex.InnerException is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Remote returned 404 for {IndexName}/{Key}", indexName, key);
                    throw new KeyNotFoundException($"Document '{key}' not found in '{indexName}'.");
                }
                _logger.LogError(ex, "Dapr invocation failed in GetByIdWithSimilarAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw;
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "GetByIdWithSimilarAsync called with null argument");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByIdWithSimilarAsync: indexName={IndexName}, key={Key}", indexName, key);
                throw;
            }
        }
    }
}