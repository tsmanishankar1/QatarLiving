using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.NewsInternalService
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
        private List<V2NewsCategory> newsCateg =
           [
                   new()
                    {
                        Id = 1,
                        CategoryName = "News",
                        SubCategories = new List<V2NewsSubCategory>() {
                            new() { Id = 1, CategoryName = "Qatar" },
                            new() { Id = 2, CategoryName = "Middle East" },
                            new() { Id = 3, CategoryName = "World" },
                            new() { Id = 4, CategoryName = "Health & Education" },
                            new() { Id = 5, CategoryName = "Community" },
                            new() { Id = 6, CategoryName = "Law" },
                        }
                    },

                    new()
                    {
                        Id = 2,
                        CategoryName = "Business",
                        SubCategories = [
                            new() { Id = 1, CategoryName = "QatarEconomy" },
                            new() { Id = 2, CategoryName = "MarketUpdates" },
                            new() { Id = 2, CategoryName = "Real Estate" },
                            new() { Id = 2, CategoryName = "Entrepreneurship" },
                            new() { Id = 2, CategoryName = "Finance" },
                            new() { Id = 2, CategoryName = "Jobs & Careers" }
                        ]
                    },

                    new()
                    {
                        Id = 3,
                        CategoryName = "Sports",
                        SubCategories = [
                            new() { Id = 1, CategoryName = "Qatar Sports" },
                            new() { Id = 2, CategoryName = "FootBall" },
                            new() { Id = 2, CategoryName = "International " },
                            new() { Id = 2, CategoryName = "MotorSports" },
                            new() { Id = 2, CategoryName = "Olympics" },
                            new() { Id = 2, CategoryName = "Athelete Features" }
                        ]
                    },

                    new()
                    {
                        Id = 4,
                        CategoryName = "LifeStyle",
                        SubCategories = [
                            new() { Id = 1, CategoryName = "Food & Dining" },
                            new() { Id = 2, CategoryName = "Travel & Leisure" },
                            new() { Id = 2, CategoryName = "Arts & Culture" },
                            new() { Id = 2, CategoryName = "Events" },
                            new() { Id = 2, CategoryName = "Fashion & Style" },
                            new() { Id = 2, CategoryName = "Home & Living" }
                        ]
                    }
           ];

        public V2InternalNewsService(DaprClient dapr, ILogger<IV2NewsService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public Task<Dictionary<string, string>> GetWriterTagsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Returning static writer tags as key-value JSON");

            var tagDict = writerTags.ToDictionary(tag => tag, tag => tag);
            return Task.FromResult(tagDict);
        }

        public async Task<List<V2NewsCategory>> GetNewsCategoriesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Returning static writer tags as key-value JSON");
            return await Task.FromResult(newsCateg);
        }
        public async Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            var slots = Enum.GetValues(typeof(NewsSlot))
                .Cast<NewsSlot>()
                .Select(s => new V2NewsSlot
                {
                    Id = (int)s,
                    Name = s.ToString()
                })
                .ToList();

            return await Task.FromResult(slots);
        }
        public async Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                dto.Id = Guid.NewGuid();
                dto.CreatedBy = userId;
                dto.UpdatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                string storeName = V2Content.ContentStoreName;
                string articleIdStr = dto.Id.ToString();

                // Save article
                await _dapr.SaveStateAsync(storeName, articleIdStr, dto, cancellationToken: cancellationToken);

                // Maintain index
                var indexKey = V2Content.NewsIndexKey;
                var currentIndex = await _dapr.GetStateAsync<List<string>>(storeName, indexKey, cancellationToken: cancellationToken)
                                  ?? new List<string>();

                if (!currentIndex.Contains(articleIdStr))
                {
                    currentIndex.Add(articleIdStr);
                    await _dapr.SaveStateAsync(storeName, indexKey, currentIndex, cancellationToken: cancellationToken);
                }

                // Process category slots
                foreach (var cat in dto.Categories)
                {
                    int slotId = cat.SlotId == 0 ? (int)Slot.UnPublished : cat.SlotId;

                    if (slotId == (int)Slot.Published || slotId == (int)Slot.UnPublished)
                    {
                        string key = GetStatusSlotKey(cat.CategoryId, cat.SubcategoryId, slotId);
                        await _dapr.SaveStateAsync(storeName, key, articleIdStr, cancellationToken: cancellationToken);
                    }
                    else if (slotId >= 1 && slotId <= 13)
                    {
                        await HandleSlotShiftAsync(cat.CategoryId, cat.SubcategoryId, slotId, dto, cancellationToken);
                    }
                    else
                    {
                        throw new InvalidDataException($"Invalid SlotId: {slotId} for category {cat.CategoryId}-{cat.SubcategoryId}");
                    }
                }

                return "News article created successfully";
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create article");
                throw new Exception("Unexpected error during article creation", ex);
            }
        }

        private async Task HandleSlotShiftAsync(int categoryId, int subCategoryId, int desiredSlot, V2NewsArticleDTO newArticle, CancellationToken cancellationToken)
        {
            const int MaxSlot = 13;
            string storeName = V2Content.ContentStoreName;

            var keys = Enumerable.Range(1, MaxSlot)
                .Select(i => GetSlotKey(categoryId, subCategoryId, i))
                .ToList();

            var existingStates = await _dapr.GetBulkStateAsync(storeName, keys, parallelism: null,
          metadata: null, cancellationToken: cancellationToken);
            var slotMap = new Dictionary<int, V2NewsArticleDTO>();

            foreach (var item in existingStates)
            {
                var match = Regex.Match(item.Key, @"slot(\d+)$");
                if (!match.Success || string.IsNullOrWhiteSpace(item.Value)) continue;

                int slotNum = int.Parse(match.Groups[1].Value);
                var article = JsonSerializer.Deserialize<V2NewsArticleDTO>(item.Value);
                if (article != null)
                    slotMap[slotNum] = article;
            }

            for (int i = MaxSlot - 1; i >= desiredSlot; i--)
            {
                if (slotMap.ContainsKey(i))
                {
                    int nextSlot = i + 1;
                    if (nextSlot > MaxSlot) continue;

                    var article = slotMap[i];
                    var fromKey = GetSlotKey(categoryId, subCategoryId, i);
                    var toKey = GetSlotKey(categoryId, subCategoryId, nextSlot);

                    var cat = article.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
                    if (cat != null) cat.SlotId = nextSlot;

                    await _dapr.SaveStateAsync(storeName, toKey, article, cancellationToken: cancellationToken);
                    await _dapr.DeleteStateAsync(storeName, fromKey, cancellationToken: cancellationToken);
                }
            }

            var newCat = newArticle.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
            if (newCat != null) newCat.SlotId = desiredSlot;

            await _dapr.SaveStateAsync(storeName, GetSlotKey(categoryId, subCategoryId, desiredSlot), newArticle, cancellationToken: cancellationToken);
        }

        private string GetSlotKey(int categoryId, int subCategoryId, int slot) =>
            $"slot-article-{categoryId}-{subCategoryId}-slot{slot}";

        private string GetStatusSlotKey(int categoryId, int subCategoryId, int slot) =>
            $"slot-article-status-{categoryId}-{subCategoryId}-slot{slot}";
        public async Task<List<V2NewsArticleDTO>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    V2Content.ContentStoreName,
                    V2Content.NewsIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                _logger.LogInformation("Fetched {Count} keys from index", keys.Count);

                var items = await _dapr.GetBulkStateAsync(V2Content.ContentStoreName, keys, null, cancellationToken: cancellationToken);

                var articles = items
                    .Select(i => JsonSerializer.Deserialize<V2NewsArticleDTO>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(dto => dto != null)
                    .ToList();

                _logger.LogInformation("Deserialized {Count} articles", articles.Count);

                return articles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all news articles");
                throw;
            }
        }
        public async Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            try
            {
                var ids = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.NewsIndexKey) ?? new();

                var stateItems = await _dapr.GetBulkStateAsync(V2Content.ContentStoreName, ids, parallelism: null, metadata: null, cancellationToken);
                return stateItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                    .Select(i => JsonSerializer.Deserialize<V2NewsArticleDTO>(i.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                    .Where(a => a?.Categories.Any(c => c.CategoryId == categoryId) == true)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching articles by category");
                throw;
            }
        }

        public async Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(int categoryId, int subCategoryId, CancellationToken cancellationToken)
        {
            try
            {
                var ids = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.NewsIndexKey) ?? new();

                var stateItems = await _dapr.GetBulkStateAsync(V2Content.ContentStoreName, ids, parallelism: null, metadata: null, cancellationToken);
                return stateItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                    .Select(i => JsonSerializer.Deserialize<V2NewsArticleDTO>(i.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                    .Where(a => a?.Categories.Any(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId) == true)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching articles by subcategory");
                throw;
            }
        }

        public async Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var StoreName = V2Content.ContentStoreName;

                if (dto.Id == Guid.Empty)
                    throw new InvalidDataException("Id is required for update");

                string key = dto.Id.ToString();
                var existing = await _dapr.GetStateAsync<V2NewsArticleDTO>(StoreName, key, cancellationToken:cancellationToken);
                if (existing == null)
                    throw new KeyNotFoundException("News article not found");

                dto.UpdatedAt = DateTime.UtcNow;

                // Optional: Handle slot logic or image updates if needed here

                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: cancellationToken);
                return "News article updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update news article");
                throw;
            }
        }
    }
}