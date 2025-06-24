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
        private static readonly List<string> writerTags = new()
    {
        "Qatar Living",
        "Everything Qatar",
        "FIFA Arab Cup",
        "QL Exclusive",
        "Advice & Help"
    };

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

                // Set publishedDate only if the status is Published
                if (dto.Status == StatusType.Published)
                {
                    dto.publishedDate = DateTime.UtcNow.ToString("o");
                }
                // Save the news item
                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    dto,
                    cancellationToken: cancellationToken
                );

                // Get or create index list
                var indexKey = ConstantValues.V2Content.NewsIndexKey;
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    indexKey
                    //cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(dto.Id.ToString()))
                {
                    keys.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
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
                var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2Content.ContentStoreName, ConstantValues.V2Content.NewsIndexKey) ?? new();

                _logger.LogInformation("Fetched {Count} keys from index", keys.Count);

                var items = await _dapr.GetBulkStateAsync(ConstantValues.V2Content.ContentStoreName, keys, null, cancellationToken: cancellationToken);

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
                    ConstantValues.V2Content.ContentStoreName,
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

            // Set publishedDate only if status changed to Published
            if (existing.Status != StatusType.Published && dto.Status == StatusType.Published)
            {
                dto.publishedDate = DateTime.UtcNow.ToString("o");
            }
            else
            {
                dto.publishedDate = existing.publishedDate;
            }

            await _dapr.SaveStateAsync(
                V2Content.ContentStoreName,
                dto.Id.ToString(),
                dto,
                cancellationToken: cancellationToken
            );

            return "News updated successfully";
        }


        public async Task<bool> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await GetNewsById(id, cancellationToken);
            if (existing == null)
                return false;

            await _dapr.DeleteStateAsync(V2Content.ContentStoreName, id.ToString(), cancellationToken: cancellationToken);

            var keys = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.NewsIndexKey) ?? new List<string>();
            keys.Remove(id.ToString());
            await _dapr.SaveStateAsync(V2Content.ContentStoreName, V2Content.NewsIndexKey, keys, cancellationToken: cancellationToken);

            return true;
        }

        public async Task<string> CreateNewsCategoryAsync(NewsCategoryDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                dto.Id = id;
                foreach(var item in dto.SubCategory)
                {
                    item.Id = Guid.NewGuid();
                }

                // Save the news item
                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    dto,
                    cancellationToken: cancellationToken
                );

                // Get or create index list
                var indexKey = ConstantValues.V2Content.NewsIndexKey;
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    indexKey
                //cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(dto.Id.ToString()))
                {
                    keys.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        indexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "News Category created successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating Category", ex);
            }
        }

        public async Task<List<NewsCategoryDto>> GetAllNewsCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2Content.ContentStoreName, ConstantValues.V2Content.NewsIndexKey) ?? new();

                _logger.LogInformation("Fetched {Count} keys from index", keys.Count);

                var items = await _dapr.GetBulkStateAsync(ConstantValues.V2Content.ContentStoreName, keys, null, cancellationToken: cancellationToken);

                var News = items
                    .Select(i => JsonSerializer.Deserialize<NewsCategoryDto>(i.Value, new JsonSerializerOptions
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


        //// new news endpoints
        public Task<Dictionary<string, string>> GetWriterTagsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Returning static writer tags as key-value JSON");

            var tagDict = writerTags.ToDictionary(tag => tag, tag => tag);
            return Task.FromResult(tagDict);
        }
        public async Task<string> CreateNewsArticleAsync(Guid userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var articleId = Guid.NewGuid();
                dto.Id = articleId;
                dto.CreatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedBy = userId;
                dto.UpdatedAt = DateTime.UtcNow;

                string storeName = "contentstatestore";
                string indexKey = "news-index";

                // Save to Dapr
                await _dapr.SaveStateAsync(storeName, dto.Id.ToString(), dto, cancellationToken: cancellationToken);

                // Update index
                var index = await _dapr.GetStateAsync<List<string>>(storeName, indexKey, cancellationToken: cancellationToken) ?? new();
                if (!index.Contains(dto.Id.ToString()))
                {
                    index.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(storeName, indexKey, index, cancellationToken: cancellationToken);
                }

                return "News article created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article");
                throw;
            }
        }
    }
}