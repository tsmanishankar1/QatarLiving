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
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalDailyService : IV2ContentDailyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalDailyService> _logger;

        private const string AppId = V2Content.ContentServiceAppId;
        private const string BaseUrl = "/api/v2/daily/dailyslots";
        public V2ExternalDailyService(DaprClient dapr, ILogger<V2ExternalDailyService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<List<DailyTopSectionSlot>> GetAllSlotsAsync(
                    CancellationToken cancellationToken = default
                )
        {
            var url = BaseUrl;
            _logger.LogDebug("GET {Url}", url);

            // GET /dailySlots
            var result = await _dapr.InvokeMethodAsync<List<DailyTopSectionSlot>>(
                HttpMethod.Get, AppId, url, cancellationToken
            );

            return result ?? new List<DailyTopSectionSlot>();
        }
       

       

        public async Task<string> UpsertSlotAsync(
                  DailyTopSectionSlot dto,
                  CancellationToken cancellationToken = default
              )
        {
            var url = BaseUrl;
            var json = JsonSerializer.Serialize(dto);

            _logger.LogDebug("POST {Url} Payload: {Json}", url, json);

            // POST /dailySlots/{slotNumber}
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
                throw new HttpRequestException($"Bad response ({(int)res.StatusCode}): {body}");
            }

            return JsonSerializer.Deserialize<string>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? throw new Exception("Empty response from upsert-slot service.");
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
        public async Task<bool> SoftDeleteDailyTopicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = $"/api/v2/daily/dailytopic/{id}";
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
        public async Task<List<DailyTopic>> GetAllDailyTopicsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/daily/dailytopics";
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

        public async Task<bool> UpdateDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/daily/dailytopic/update";
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
                var path = "/api/v2/daily/publishstatusbyid"; // Match this with your endpoint path
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                // Create a lightweight payload object
                var payload = new
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

                _logger.LogInformation("Successfully updated publish status for DailyTopic ID {Id} to {Status}", id, isPublished);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating publish status for DailyTopic ID {Id}", id);
                throw;
            }
        }


    }
}
