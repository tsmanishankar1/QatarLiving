using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service
{
    public class V2InternalNewsService : IV2NewsService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<IV2NewsService> _logger;
        public V2InternalNewsService(DaprClient dapr, ILogger<IV2NewsService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateNews(V2ContentNewsDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                dto.Id = id;
                dto.date_created = DateTime.UtcNow.ToString("o"); // ISO 8601 format

                // Save the news item
                await _dapr.SaveStateAsync(
                    ConstantValues.V2ContentNews.ContentStoreName,
                    dto.Id.ToString(),
                    dto,
                    cancellationToken: cancellationToken
                );

                // Get or create index list
                var indexKey = ConstantValues.V2ContentNews.NewsIndexKey;
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2ContentNews.ContentStoreName,
                    indexKey
                    //cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(dto.Id.ToString()))
                {
                    keys.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2ContentNews.ContentStoreName,
                        indexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "News created successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating news", ex);
            }


        }

        public async Task<List<V2ContentNewsDto>> GetAllNews(CancellationToken cancellationToken)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2ContentNews.ContentStoreName, ConstantValues.V2ContentNews.NewsIndexKey) ?? new();

                _logger.LogInformation("Fetched {Count} keys from index", keys.Count);

                var items = await _dapr.GetBulkStateAsync(ConstantValues.V2ContentNews.ContentStoreName, keys, null, cancellationToken: cancellationToken);

                var News = items
                    .Select(i => JsonSerializer.Deserialize<V2ContentNewsDto>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(e => e != null)
                    .ToList();

                _logger.LogInformation("Deserialized {Count} news items", News.Count);
                return News;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all news items");
                throw;
            }
        }


        public async Task<V2ContentNewsDto?> GetNewsById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await _dapr.GetStateAsync<V2ContentNewsDto>(
                    ConstantValues.V2ContentNews.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken
                );

                if (data == null || data.Id == Guid.Empty)
                {
                    _logger.LogWarning("News not found for ID {Id}", id);
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching news by ID {Id}", id);
                throw;
            }
        }

        public async Task<string> UpdateNews(V2ContentNewsDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await GetNewsById(dto.Id, cancellationToken);
            if (existing == null)
                throw new InvalidDataException("News not found.");

            await _dapr.SaveStateAsync(V2ContentNews.ContentStoreName, dto.Id.ToString(), dto, cancellationToken: cancellationToken);
            return "News updated successfully";
        }

        public async Task<bool> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await GetNewsById(id, cancellationToken);
            if (existing == null)
                return false;

            await _dapr.DeleteStateAsync(V2ContentNews.ContentStoreName, id.ToString(), cancellationToken: cancellationToken);

            var keys = await _dapr.GetStateAsync<List<string>>(V2ContentNews.ContentStoreName, V2ContentNews.NewsIndexKey) ?? new List<string>();
            keys.Remove(id.ToString());
            await _dapr.SaveStateAsync(V2ContentNews.ContentStoreName, V2ContentNews.NewsIndexKey, keys, cancellationToken: cancellationToken);

            return true;
        }
    }
}