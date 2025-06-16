using System;
using System.Net.Http;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;

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
        /// Calls SearchService’s POST /api/{vertical}/search
        /// </summary>
        public async Task<CommonSearchResponse> SearchAsync(string vertical, CommonSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            try
            {
                var methodName = $"/api/{vertical}/search";
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
                _logger.LogError(ex, "Dapr invocation failed in SearchAsync: vertical={Vertical}", vertical);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchAsync: vertical={Vertical}", vertical);
                throw;
            }
        }

        /// <summary>
        /// Calls SearchService’s POST /api/{vertical}/upload
        /// </summary>
        public async Task<string> UploadAsync(CommonIndexRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                var vertical = request.VerticalName;
                var methodName = $"/api/{vertical}/upload";
                var result = await _dapr.InvokeMethodAsync<CommonIndexRequest, string>(
                    HttpMethod.Post,
                    appId: SERVICE_APP_ID,
                    methodName: methodName,
                    request
                );

                return result ?? string.Empty;
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
        /// Calls SearchService’s GET /api/{vertical}/{key}
        /// </summary>
        public async Task<T?> GetByIdAsync<T>(string vertical, string key)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
                var methodName = $"/api/{vertical}/{key}";
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
                _logger.LogError(ex, "Dapr invocation failed in GetByIdAsync: vertical={Vertical}, key={Key}", vertical, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByIdAsync: vertical={Vertical}, key={Key}", vertical, key);
                throw;
            }
        }

        /// <summary>
        /// Calls SearchService’s Delete /api/{vertical}/{key}
        /// </summary>
        public async Task DeleteAsync(string vertical, string key)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            try
            {
                var methodName = $"/api/{vertical}/{key}";
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    appId: SERVICE_APP_ID,
                    methodName: methodName
                );
                _logger.LogInformation("Deleted document '{Key}' from vertical '{Vertical}'", key, vertical);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "DeleteAsync called with null argument: {Param}", ex.ParamName);
                throw;
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex, "Dapr invocation failed in DeleteAsync: vertical={Vertical}, key={Key}", vertical, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteAsync: vertical={Vertical}, key={Key}", vertical, key);
                throw;
            }
        }
    }
}
