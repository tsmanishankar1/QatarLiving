using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Backend.API.Service.V2ClassifiedBoService;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
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
        private readonly IClassifiedService _classifiedService;
        private readonly IV2SubscriptionService _subscriptionContext;
        public ExternalClassifiedPreLovedBOService(
            DaprClient dapr,
            ILogger<ExternalClassifiedPreLovedBOService> logger,
            IHttpContextAccessor httpContextAccessor,
            IFileStorageBlobService fileStorageBlob,
            ISearchService searchService,IClassifiedService classifiedService, IV2SubscriptionService subscriptionContext)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
            _fileStorageBlob = fileStorageBlob;
            _searchService = searchService;
            _classifiedService = classifiedService;
            _subscriptionContext = subscriptionContext;
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

        public async Task<ClassifiedBOPageResponse<PreLovedViewP2PDto>> ViewPreLovedP2PSubscriptions(string? Status, string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved p2p external services initiated.");
                var queryParams = $"?Status={Status}&createdDate={createdDate}&publishedDate={publishedDate}&Page={Page}&PageSize={PageSize}&Search={Search}&SortBy={SortBy}&SortOrder={SortOrder}";
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

        public async Task<string> BulkEditP2PSubscriptions(BulkEditPreLovedP2PDto dto, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                int TotalCount = 0;
                int failedCount = 0;

                if (dto.AdIds == null || !dto.AdIds.Any())
                    throw new ArgumentException("AdIds cannot be null or empty.");

                if (dto.AdStatus == null)
                    throw new ArgumentException("AdStatus is required.");

                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId cannot be null or empty.");
                List<long> processedadIds = new List<long>();
                processedadIds = dto.AdIds;

                if (dto.AdIds.Count > 0)
                {
                    TotalCount = dto.AdIds.Count();

                    var ads = await Task.WhenAll(
               dto.AdIds.Select(id => _classifiedService.GetPrelovedAdById(id, cancellationToken))
           );
                    var groupedActions = ads
               .Where(a => a != null && a.SubscriptionId.HasValue && a.SubscriptionId.Value != Guid.Empty)
               .GroupBy(a => new
               {
                   a.SubscriptionId,
                   ActionType = dto.AdStatus
               })
               .ToList();


                    foreach (var group in groupedActions)
                    {
                        var subscriptionId = group.Key.SubscriptionId!.Value;
                        var actionName = group.Key.ActionType switch
                        {
                            BulkActionEnum.Promote => "promote", // match single method case
                            BulkActionEnum.Feature => "feature",
                            BulkActionEnum.Publish => "publish",
                            BulkActionEnum.Unpublish => "unpublish",
                            _ => throw new InvalidOperationException("Invalid bulk action.")
                        };

                        var usageCount = ads.Count(x => x.SubscriptionId == subscriptionId);

                        var canUse = await _subscriptionContext.ValidateSubscriptionUsageAsync(
                                subscriptionId,
                                actionName,
                                usageCount,
                                cancellationToken
                            );

                        if (!canUse)
                        {
                            failedCount = failedCount + usageCount;
                            List<long> RemovedAds = new List<long>();
                            RemovedAds = ads.Where(x => x.SubscriptionId == subscriptionId).Select(x => x.Id).ToList();
                            if (RemovedAds != null)
                            {
                                if (RemovedAds.Count > 0)
                                {
                                    foreach (var item in RemovedAds)
                                    {
                                        processedadIds.Remove(item);
                                    }
                                }
                            }
                        }
                    }
                }

                dto.AdIds = processedadIds;
                var url = $"/api/v2/classifiedbo/preloved-bulk-edits-subscriptions/{userId}";
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

                if (dto.AdIds.Count > 0)
                {
                    var successAds = await Task.WhenAll(
                    dto.AdIds.Select(id => _classifiedService.GetPrelovedAdById(id, cancellationToken)));

                    var successgroupedActions = successAds
               .Where(a => a != null && a.SubscriptionId.HasValue && a.SubscriptionId.Value != Guid.Empty)
               .GroupBy(a => new
               {
                   a.SubscriptionId,
                   ActionType = dto.AdStatus
               })
               .ToList();


                    foreach (var group in successgroupedActions)
                    {
                        var subscriptionId = group.Key.SubscriptionId!.Value;
                        var actionName = group.Key.ActionType switch
                        {
                            BulkActionEnum.Promote => "Promote",
                            BulkActionEnum.Feature => "Feature",
                            BulkActionEnum.Publish => "Publish",
                            BulkActionEnum.Unpublish => "Unpublish",
                            _ => throw new InvalidOperationException("Invalid bulk action.")
                        };

                        var usageCount = successAds.Count(x => x.SubscriptionId == subscriptionId);

                        var success = await _subscriptionContext.RecordSubscriptionUsageAsync(
                            subscriptionId,
                            actionName,
                            usageCount,
                            cancellationToken
                        );

                        if (!success)
                        {
                            failedCount++;
                        }
                    }
                }
            
                
                int Success = TotalCount - failedCount;
                return "Preloved status updated "+Success.ToString()+" out of "+ TotalCount.ToString()+" successfully.";
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error in editing preloved status information.");
                throw;
            }
        }
        private static long GuidToLong(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return BitConverter.ToInt64(bytes, 0); // uses first 8 bytes
        }
    }
}
