using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Backend.API.Service.V2ClassifiedBoService;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using System.Net;
using System.Text;
using System.Text.Json;
namespace QLN.Backend.API.Service.ClassifiedBoService
{
    public class ExternalClassifiedPreLovedBOService : IClassifiedPreLovedBOService
    {
        private const string SERVICE_APP_ID = ConstantValues.ServiceAppIds.ClassifiedServiceApp;
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalClassifiedPreLovedBOService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileStorageBlobService _fileStorageBlob;
        private readonly ISearchService _searchService;

        public ExternalClassifiedPreLovedBOService(
            DaprClient dapr,
            ILogger<ExternalClassifiedPreLovedBOService> logger,
            IHttpContextAccessor httpContextAccessor,
            IFileStorageBlobService fileStorageBlob,
            ISearchService searchService)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
            _searchService = searchService;
        }


        public async Task<ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>> ViewPreLovedSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved external services initiated.");
                var queryParams = $"?subscriptionType={subscriptionType}&filterDate={filterDate}&Page={Page}&PageSize={PageSize}&Search={Search}&SortBy={SortBy}&SortOrder={SortOrder}";
                var response = await _dapr.InvokeMethodAsync<ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,

                    $"api/v2/classifiedbo/preloved-view-subscriptions{queryParams}",
                    cancellationToken
                );
                _logger.LogInformation("Preloved external services got response.");
                return response ?? new ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in preloved subscriptions.");
                throw new InvalidOperationException("Error fetching preloved subscriptions.", ex);
            }
        }

        public async Task<ClassifiedBOPageResponse<PreLovedViewP2PDto>> ViewPreLovedP2PSubscriptions(string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved p2p external services initiated.");
                var queryParams = $"?createdDate={createdDate}&publishedDate={publishedDate}&Page={Page}&PageSize={PageSize}&Search={Search}&SortBy={SortBy}&SortOrder={SortOrder}";
                var response = await _dapr.InvokeMethodAsync<ClassifiedBOPageResponse<PreLovedViewP2PDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,

                    $"api/v2/classifiedbo/preloved-p2p-subscriptions{queryParams}",
                    cancellationToken
                );
                _logger.LogInformation("Preloved p2p external services got response.");
                return response ?? new ClassifiedBOPageResponse<PreLovedViewP2PDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in preloved p2p subscriptions.");
                throw new InvalidOperationException("Error fetching preloved p2p subscriptions.", ex);
            }
        }

        public async Task<ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>> ViewPreLovedP2PTransactions(string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved p2p external services initiated.");
                var queryParams = $"?createdDate={createdDate}&publishedDate={publishedDate}&Page={Page}&PageSize={PageSize}&Search={Search}&SortBy={SortBy}&SortOrder={SortOrder}";
                var response = await _dapr.InvokeMethodAsync<ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,

                    $"api/v2/classifiedbo/preloved-p2p-transactions{queryParams}",
                    cancellationToken
                );
                _logger.LogInformation("Preloved p2p external services got response.");
                return response ?? new ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in preloved p2p transactions.");
                throw new InvalidOperationException("Error fetching preloved p2p transactions.", ex);
            }
        }

        public async Task<string> BulkEditP2PSubscriptions(BulkEditPreLovedP2PDto dto, CancellationToken cancellationToken = default)
        {
            try
            {

                var url = "/api/v2/classifiedbo/preloved-bulk-edits-subscriptions";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, SERVICE_APP_ID, url);
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
                return "Preloved status updated successfully.";
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error in editing preloved status information.");
                throw;
            }
        }

    }
}
