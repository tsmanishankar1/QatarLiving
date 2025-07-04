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
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System.Net;
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
        private const string BaseUrl = "/api/v2/daily";

        public V2ExternalDailyService(DaprClient dapr, ILogger<V2ExternalDailyService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateDailyTopicAsync(
            string userId,
            DailyTopSectionSlot dto,
            CancellationToken cancellationToken = default
        )
        {
            // stamp required fields
            dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
            dto.CreatedBy = userId;
            dto.UpdatedBy = userId;
            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;

            var url = $"{BaseUrl}/dailyTopics";
            var json = JsonSerializer.Serialize(dto);
            _logger.LogDebug("POST {Url} Payload: {Json}", url, json);

            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("CreateDailyTopicAsync → {StatusCode} {Reason}\nResponse: {Body}",
                    (int)res.StatusCode, res.ReasonPhrase, body);
                throw new HttpRequestException($"Bad response ({(int)res.StatusCode}): {body}");
            }

            return JsonSerializer.Deserialize<string>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? throw new Exception("Empty response from daily-content service.");
        }

        public async Task<List<DailyTopicContent>> GetAllDailyTopicsAsync(
            CancellationToken cancellationToken = default
        )
        {
            var url = $"{BaseUrl}/dailyTopics";
            return await _dapr.InvokeMethodAsync<List<DailyTopicContent>>(
                HttpMethod.Get, AppId, url, cancellationToken
            ) ?? new List<DailyTopicContent>();
        }

        public async Task<DailyTopicContent?> GetDailyTopicByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            var url = $"{BaseUrl}/dailyTopics/{id}";
            var res = await _dapr.InvokeMethodWithResponseAsync(
                _dapr.CreateInvokeMethodRequest(HttpMethod.Get, AppId, url),
                cancellationToken
            );

            if (res.StatusCode == HttpStatusCode.NotFound)
                return null;

            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DailyTopicContent>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public async Task<List<DailyTopSectionSlot>> GetAllDailySlotsAsync(
            Guid topicId,
            CancellationToken cancellationToken = default
        )
        {
            var url = $"{BaseUrl}/dailyTopics/{topicId}/slots";
            return await _dapr.InvokeMethodAsync<List<DailyTopSectionSlot>>(
                HttpMethod.Get, AppId, url, cancellationToken
            ) ?? new List<DailyTopSectionSlot>();
        }
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalDailyService> _logger;
        private readonly IFileStorageBlobService _blobStorage;

        public V2ExternalDailyService(
           DaprClient dapr,
           ILogger<V2ExternalDailyService> logger,
           IFileStorageBlobService blobStorage)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
        }

        public async Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/daily/dailytopicById";
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




        public async Task<string> UpsertDailySlotAsync(
            string userId,
            Guid topicId,
            int slotNumber,
            DailyTopSectionSlot dto,
            CancellationToken cancellationToken = default
        )
        {
            // stamp metadata
            dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
            dto.SlotNumber = slotNumber;
            dto.SlotType = (DailySlotType)slotNumber;
            dto.CreatedBy = userId;
            dto.UpdatedBy = userId;
            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;

            var url = $"{BaseUrl}/dailyTopics/{topicId}/slots/{slotNumber}";
            var json = JsonSerializer.Serialize(dto);
            _logger.LogDebug("PUT {Url} Payload: {Json}", url, json);

            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, AppId, url);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("UpsertDailySlotAsync → {StatusCode} {Reason}\nResponse: {Body}",
                    (int)res.StatusCode, res.ReasonPhrase, body);
                throw new HttpRequestException($"Bad response ({(int)res.StatusCode}): {body}");
            }

            return JsonSerializer.Deserialize<string>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? throw new Exception("Empty response from daily-slot upsert service.");
        }
    }
}
