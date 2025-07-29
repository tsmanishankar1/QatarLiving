using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Backend.API.Service.V2ContentService

{

    public class V2ExternalDailyService : IV2ContentDailyService

    {

        private readonly DaprClient _dapr;

        private readonly ILogger<V2ExternalDailyService> _logger;

        private const string AppId = V2Content.ContentServiceAppId;

        private const string BaseUrl = "/api/v2/dailyliving/topsection";

        public V2ExternalDailyService(DaprClient dapr, ILogger<V2ExternalDailyService> logger)

        {

            _dapr = dapr;

            _logger = logger;

        }
        public async Task<string> UpsertSlotAsync(string userId, DailyTopSectionSlot dto, CancellationToken cancellationToken = default)

        {

            var url = $"{BaseUrl}/{userId}";

            var json = JsonSerializer.Serialize(dto);

            _logger.LogDebug("POST {Url} Payload: {Json}", url, json);

            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);

            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);

            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)

            {

                _logger.LogError(

                    "UpsertSlotAsync → {StatusCode} {Reason}\nResponse: {Body}",

                    (int)res.StatusCode, res.ReasonPhrase, body

                );

                throw new DaprServiceException((int)res.StatusCode, body);

            }

            return JsonSerializer.Deserialize<string>(body, new JsonSerializerOptions

            {

                PropertyNameCaseInsensitive = true

            })!;

        }
        public async Task<List<DailyTopSectionSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)

        {

            var url = BaseUrl;

            _logger.LogDebug("GET {Url}", url);

            var result = await _dapr.InvokeMethodAsync<List<DailyTopSectionSlot>>(

                HttpMethod.Get, AppId, url, cancellationToken

            );

            return result ?? new List<DailyTopSectionSlot>();

        }
        public async Task<List<V2NewsArticleDTO>> GetUnusedDailyTopSectionArticlesAsync(int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            var queryParams = new Dictionary<string, string>();

            if (page.HasValue)
                queryParams.Add("page", page.Value.ToString());

            if (pageSize.HasValue)
                queryParams.Add("pageSize", pageSize.Value.ToString());

            var queryString = queryParams.Count > 0
                ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                : string.Empty;

            var path = $"/api/v2/dailyliving/topsection/unusedarticles{queryString}";

            try
            {
                return await _dapr.InvokeMethodAsync<List<V2NewsArticleDTO>>(
                    HttpMethod.Get,
                    AppId,
                    path,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }
        }
        public async Task<List<DailyTopic>> GetAllDailyTopicsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/dailytopics";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, appId, path);
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch topics. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                    response.EnsureSuccessStatusCode();
                }

                var topics = JsonSerializer.Deserialize<List<DailyTopic>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return topics ?? new List<DailyTopic>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching DailyTopics");
                throw;
            }
        }
        public async Task<string> CreateContentAsync(string userId, DailyTopicContent dto, CancellationToken ct = default)
        {
            var url = $"/api/v2/dailyliving/topic/contentbyid/{userId}";
            dto.CreatedBy = userId;
            dto.UpdatedBy = userId;
            var payload = JsonSerializer.Serialize(dto);
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new DaprServiceException((int)res.StatusCode, body);
            return JsonSerializer.Deserialize<string>(body)!;
        }
        public async Task<List<DailyTopicContent>> GetSlotsByTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/dailyliving/topic/content?topicId={topicId}";
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, AppId, url);

            _logger.LogDebug("GET {Url} with TopicId={TopicId}", url, topicId);

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "GetSlotsByTopicAsync → {StatusCode} {ReasonPhrase}\n{Body}",
                    (int)res.StatusCode, res.ReasonPhrase, body
                );
                throw new DaprServiceException((int)res.StatusCode, body);
            }

            var list = JsonSerializer.Deserialize<List<DailyTopicContent>>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return list ?? new List<DailyTopicContent>();
        }
        public async Task<string> ReorderSlotsBatchAsync(string userId, DailyTopicSlotReorderRequest dto, CancellationToken ct)
        {
            var url = $"/api/v2/dailyliving/topic/content/reorderbyid/{userId}";
            var payload = JsonSerializer.Serialize(dto);
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new DaprServiceException((int)res.StatusCode, body);
            return JsonSerializer.Deserialize<string>(body)!;
        }
        public async Task<string> DeleteContentAsync(Guid contentId, CancellationToken ct)
        {
            var url = $"/api/v2/dailyliving/topic/content/{contentId}";
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Delete, AppId, url);

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new DaprServiceException((int)res.StatusCode, body);
            return JsonSerializer.Deserialize<string>(body)!;
        }
        public async Task<bool> DeleteDailyTopicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = $"/api/v2/dailyliving/dailytopic/{id}";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Delete, appId, path);
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to soft delete DailyTopic {Id}. Status: {Status}. Response: {Body}", id, response.StatusCode, responseBody);
                    return false;
                }

                _logger.LogInformation("Successfully soft deleted DailyTopic {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during soft delete of DailyTopic {Id}", id);
                throw;
            }
        }
        public async Task<bool> UpdateDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/dailytopicupdateid";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(topic), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to update topic. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update DailyTopic");
                throw;
            }
        }
        public async Task<bool> UpdatePublishStatusAsync(Guid id, bool isPublished, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/publishstatusbyid"; // Internal endpoint without auth
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                // Compose payload as DailyTopic (you can also use a separate DTO if needed)
                var payload = new DailyTopic
                {
                    Id = id,
                    IsPublished = isPublished
                };

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to update publish status. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                    return false;
                }

                _logger.LogInformation("Successfully updated publish status for topic ID: {Id} to {Status}", id, isPublished);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating publish status for DailyTopic with ID {Id}", id);
                throw;
            }
        }
        public async Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/dailytopicById";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(topic), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create topic. Status: {Status}, Content: {Body}", response.StatusCode, responseBody);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create DailyTopic");
                throw;
            }
        }
        public async Task<List<V2NewsArticleDTO>> GetUnusedNewsArticlesForTopicAsync(Guid topicId, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            if (topicId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(topicId), "TopicId cannot be empty.");

            var queryParams = new Dictionary<string, string>();

            if (page.HasValue)
                queryParams.Add("page", page.Value.ToString());

            if (pageSize.HasValue)
                queryParams.Add("pageSize", pageSize.Value.ToString());

            var queryString = queryParams.Count > 0
                ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                : string.Empty;


            var path = $"/api/v2/dailyliving/topic/{topicId}/unusedarticles{queryString}";

            try
            {
                return await _dapr.InvokeMethodAsync<List<V2NewsArticleDTO>>(
                    HttpMethod.Get,
                    AppId,
                    path,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }
        }
        public async Task<ContentsDailyPageResponse> GetDailyLivingLandingAsync(CancellationToken ct)
        {
            var path = "/api/v2/dailyliving/landing";
            var appId = V2Content.ContentServiceAppId;
            return await _dapr.InvokeMethodAsync<ContentsDailyPageResponse>(
                HttpMethod.Get,
                appId,
                path,
                cancellationToken: ct
            );
        }
    }

}

