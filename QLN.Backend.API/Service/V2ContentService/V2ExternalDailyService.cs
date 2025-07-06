using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
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
            var url = $"/api/v2/dailyliving/topic/content";
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, AppId, url);
            req.Headers.Add("TopicId", topicId.ToString());

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
        public async Task<string> ReorderSlotsAsync(string userId, ReorderDailyTopicContentDto dto, CancellationToken ct)
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


    }
}
