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

        public V2InternalNewsService(DaprClient dapr, ILogger<IV2NewsService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public Task<WriterTagsResponse> GetWriterTagsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Returning static writer tags as key-value JSON");

            var response = new WriterTagsResponse
            {
                Tags = writerTags
            };

            return Task.FromResult(response);
        }

        public async Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            var slots = Enum.GetValues(typeof(Slot))
                .Cast<Slot>()
                .Select(s => new V2NewsSlot
                {
                    Id = (int)s,
                    Name = s.ToString()
                })
                .ToList();

            return await Task.FromResult(slots);
        }


        private string GenerateNewsSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            var slug = title.Trim().ToLower()
                             .Replace(" ", "-")
                             .Replace("--", "-");
            return slug;
        }
        public async Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var slug = GenerateNewsSlug(dto.Title);
                dto.Slug = slug;
                dto.Id = Guid.NewGuid();
                dto.CreatedBy = userId;
                dto.UpdatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                string storeName = V2Content.ContentStoreName;
                string articleIdStr = dto.Id.ToString();

                await _dapr.SaveStateAsync(storeName, articleIdStr, dto, cancellationToken: cancellationToken);

                var indexKey = V2Content.NewsIndexKey;
                var currentIndex = await _dapr.GetStateAsync<List<string>>(storeName, indexKey, cancellationToken: cancellationToken)
                                  ?? new List<string>();

                if (!currentIndex.Contains(articleIdStr))
                {
                    currentIndex.Add(articleIdStr);
                    await _dapr.SaveStateAsync(storeName, indexKey, currentIndex, cancellationToken: cancellationToken);
                }

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


        private async Task<string> HandleSlotShiftAsync(int categoryId, int subCategoryId, int desiredSlot, V2NewsArticleDTO newArticle, CancellationToken cancellationToken)
        {
            const int MaxSlot = 13; // Maximum slots
            string storeName = V2Content.ContentStoreName;

            var desiredSlotKey = GetSlotKey(categoryId, subCategoryId, desiredSlot);
            var existingInDesiredSlot = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, desiredSlotKey, cancellationToken: cancellationToken);

            if (existingInDesiredSlot == null)
            {
                var newCat = newArticle.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
                if (newCat != null) newCat.SlotId = desiredSlot;

                await _dapr.SaveStateAsync(storeName, desiredSlotKey, newArticle, cancellationToken: cancellationToken);
                return $"News article placed in slot {desiredSlot} successfully.";
            }

            int emptySlot = -1;
            for (int i = desiredSlot + 1; i <= MaxSlot; i++)
            {
                var slotKey = GetSlotKey(categoryId, subCategoryId, i);
                var articleInSlot = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, slotKey, cancellationToken: cancellationToken);
                if (articleInSlot == null)
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot == -1)
            {
                var lastSlotKey = GetSlotKey(categoryId, subCategoryId, MaxSlot);
                var lastSlotArticle = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, lastSlotKey, cancellationToken: cancellationToken);

                if (lastSlotArticle != null)
                {
                    var lastArticleCat = lastSlotArticle.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
                    if (lastArticleCat != null) lastArticleCat.SlotId = (int)Slot.Published;

                    string publishedKey = GetStatusSlotKey(categoryId, subCategoryId, (int)Slot.Published);
                    await _dapr.SaveStateAsync(storeName, publishedKey, lastSlotArticle.Id.ToString(), cancellationToken: cancellationToken);

                    await _dapr.SaveStateAsync(storeName, lastSlotArticle.Id.ToString(), lastSlotArticle, cancellationToken: cancellationToken);
                }

                emptySlot = MaxSlot;
            }

            var updatedSlots = new List<int>();

            for (int currentSlot = emptySlot - 1; currentSlot >= desiredSlot; currentSlot--)
            {
                var currentSlotKey = GetSlotKey(categoryId, subCategoryId, currentSlot);
                var nextSlotKey = GetSlotKey(categoryId, subCategoryId, currentSlot + 1);

                var articleToMove = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, currentSlotKey, cancellationToken: cancellationToken);

                if (articleToMove != null)
                {
                    var articleCat = articleToMove.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
                    if (articleCat != null) articleCat.SlotId = currentSlot + 1;

                    await _dapr.SaveStateAsync(storeName, nextSlotKey, articleToMove, cancellationToken: cancellationToken);

                    await _dapr.SaveStateAsync(storeName, articleToMove.Id.ToString(), articleToMove, cancellationToken: cancellationToken);

                    await _dapr.DeleteStateAsync(storeName, currentSlotKey, cancellationToken: cancellationToken);

                    updatedSlots.Add(currentSlot + 1);
                }
            }

            var newArticleCat = newArticle.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
            if (newArticleCat != null) newArticleCat.SlotId = desiredSlot;

            await _dapr.SaveStateAsync(storeName, desiredSlotKey, newArticle, cancellationToken: cancellationToken);
            updatedSlots.Add(desiredSlot);

            updatedSlots.Sort();

            return $"News article placed in slot {desiredSlot} successfully. Updated slots: {string.Join(", ", updatedSlots)}";
        }
        private string GetSlotKey(int categoryId, int subCategoryId, int slot) =>
           $"slot-article-{categoryId}-{subCategoryId}-slot{slot}";

        private string GetStatusSlotKey(int categoryId, int subCategoryId, int slot) =>
            $"slot-article-status-{categoryId}-{subCategoryId}-slot{slot}";

        public async Task<PagedResponse<V2NewsArticleDTO>> GetAllNewsArticlesAsync(
       int? page, int? perPage, string? search, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all article keys
                var keys = await _dapr.GetStateAsync<List<string>>(
                    V2Content.ContentStoreName,
                    V2Content.NewsIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                if (!keys.Any())
                    return new PagedResponse<V2NewsArticleDTO> { Page = 1, PerPage = 10, TotalCount = 0, Items = [] };

                // Fetch all articles
                var items = await _dapr.GetBulkStateAsync(V2Content.ContentStoreName, keys, null, cancellationToken: cancellationToken);
                var allArticles = items
                    .Select(i => JsonSerializer.Deserialize<V2NewsArticleDTO>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(dto => dto != null)
                    .ToList();

                // Search (if given)
                if (!string.IsNullOrWhiteSpace(search))
                    allArticles = allArticles.Where(x => x.Title != null && x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

                // Pagination
                int currentPage = Math.Max(1, page ?? 1);
                int itemsPerPage = Math.Max(1, Math.Min(100, perPage ?? 10));
                int totalCount = allArticles.Count;
                int totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);

                if (currentPage > totalPages && totalPages > 0)
                    currentPage = totalPages;

                var paginated = allArticles
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                return new PagedResponse<V2NewsArticleDTO>
                {
                    Page = currentPage,
                    PerPage = itemsPerPage,
                    TotalCount = totalCount,
                    Items = paginated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged news articles");
                throw;
            }
        }

        public async Task<List<V2NewsArticleDTO>> GetAllNewsFilterArticles(bool? isActive = null, CancellationToken cancellationToken = default)
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

                if (isActive.HasValue)
                {
                    articles = articles.Where(a => a.IsActive == isActive.Value).ToList();
                    _logger.LogInformation("Filtered to {Count} articles with IsActive = {IsActive}", articles.Count, isActive.Value);
                }

                return articles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all news articles");
                throw;
            }
        }

        public async Task<string> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _dapr.GetStateAsync<V2NewsArticleDTO>(
                ConstantValues.V2Content.ContentStoreName,
                id.ToString(),
                cancellationToken: cancellationToken);

            if (existing == null)
                throw new KeyNotFoundException($"News with ID '{id}' not found.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                ConstantValues.V2Content.ContentStoreName,
                id.ToString(),
                existing,
                new StateOptions { Consistency = ConsistencyMode.Strong },
                cancellationToken: cancellationToken);

            return "News Soft Deleted Successfully";
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
                var existing = await _dapr.GetStateAsync<V2NewsArticleDTO>(StoreName, key, cancellationToken: cancellationToken);
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
        public async Task<string> ReorderSlotsAsync(ReorderSlotRequestDto dto, CancellationToken cancellationToken)
        {
            const int MaxSlot = 13;

            if (dto.FromSlot < 1 || dto.FromSlot > MaxSlot || dto.ToSlot < 1 || dto.ToSlot > MaxSlot)
                throw new InvalidDataException("FromSlot and ToSlot must be between 1 and 13.");

            if (dto.FromSlot == dto.ToSlot)
                return $"No changes needed. Article is already in slot {dto.ToSlot}.";

            string storeName = V2Content.ContentStoreName;

            var fromKey = GetSlotKey(dto.CategoryId, dto.SubCategoryId, dto.FromSlot);
            var fromArticle = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, fromKey, cancellationToken: cancellationToken);
            if (fromArticle == null)
                throw new InvalidDataException($"No article found in slot {dto.FromSlot}.");

            var updatedSlots = new List<int>();

            if (dto.FromSlot < dto.ToSlot)
            {
                // Move down: shift up articles from FromSlot+1 to ToSlot
                for (int i = dto.FromSlot + 1; i <= dto.ToSlot; i++)
                {
                    var currentKey = GetSlotKey(dto.CategoryId, dto.SubCategoryId, i);
                    var previousKey = GetSlotKey(dto.CategoryId, dto.SubCategoryId, i - 1);
                    var article = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, currentKey, cancellationToken: cancellationToken);
                    if (article != null)
                    {
                        var cat = article.Categories.FirstOrDefault(c => c.CategoryId == dto.CategoryId && c.SubcategoryId == dto.SubCategoryId);
                        if (cat != null) cat.SlotId = i - 1;

                        await _dapr.SaveStateAsync(storeName, previousKey, article, cancellationToken: cancellationToken);
                        await _dapr.SaveStateAsync(storeName, article.Id.ToString(), article, cancellationToken: cancellationToken);
                        updatedSlots.Add(i - 1);
                    }
                }
            }
            else
            {
                // Move up: shift down articles from FromSlot-1 to ToSlot
                for (int i = dto.FromSlot - 1; i >= dto.ToSlot; i--)
                {
                    var currentKey = GetSlotKey(dto.CategoryId, dto.SubCategoryId, i);
                    var nextKey = GetSlotKey(dto.CategoryId, dto.SubCategoryId, i + 1);
                    var article = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, currentKey, cancellationToken: cancellationToken);
                    if (article != null)
                    {
                        var cat = article.Categories.FirstOrDefault(c => c.CategoryId == dto.CategoryId && c.SubcategoryId == dto.SubCategoryId);
                        if (cat != null) cat.SlotId = i + 1;

                        await _dapr.SaveStateAsync(storeName, nextKey, article, cancellationToken: cancellationToken);
                        await _dapr.SaveStateAsync(storeName, article.Id.ToString(), article, cancellationToken: cancellationToken);
                        updatedSlots.Add(i + 1);
                    }
                }
            }

            // Place original article in new slot
            var toKey = GetSlotKey(dto.CategoryId, dto.SubCategoryId, dto.ToSlot);
            var fromCat = fromArticle.Categories.FirstOrDefault(c => c.CategoryId == dto.CategoryId && c.SubcategoryId == dto.SubCategoryId);
            if (fromCat != null) fromCat.SlotId = dto.ToSlot;

            await _dapr.SaveStateAsync(storeName, toKey, fromArticle, cancellationToken: cancellationToken);
            await _dapr.SaveStateAsync(storeName, fromArticle.Id.ToString(), fromArticle, cancellationToken: cancellationToken);
            await _dapr.DeleteStateAsync(storeName, fromKey, cancellationToken: cancellationToken);
            updatedSlots.Add(dto.ToSlot);

            updatedSlots.Sort();

            return $"Reordered successfully. Updated slots: {string.Join(", ", updatedSlots)}";
        }


        public async Task<V2NewsArticleDTO?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dapr.GetStateAsync<V2NewsArticleDTO>(V2Content.ContentStoreName, id.ToString(), cancellationToken: cancellationToken);
        }

        public async Task<V2NewsArticleDTO?> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    V2Content.ContentStoreName,
                    V2Content.NewsIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                _logger.LogInformation("Fetched {Count} keys from index for slug lookup", keys.Count);

                var items = await _dapr.GetBulkStateAsync(
                    V2Content.ContentStoreName,
                    keys,
                    parallelism: null,
                    cancellationToken: cancellationToken);

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Value))
                        continue;

                    var article = JsonSerializer.Deserialize<V2NewsArticleDTO>(item.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (article is not null && string.Equals(article.Slug, slug, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Article found for slug: {Slug}", slug);
                        return article;
                    }
                }

                _logger.LogWarning("No article found with slug: {Slug}", slug);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving article by slug: {Slug}", slug);
                throw;
            }
        }

        //Category


        public async Task<List<V2NewsCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.NewsCategoryIndexKey, cancellationToken: cancellationToken)
                       ?? new List<string>();

            _logger.LogInformation("Retrieved {Count} category keys", keys.Count);

            var stateItems = await _dapr.GetBulkStateAsync(V2Content.ContentStoreName, keys, null, cancellationToken: cancellationToken);

            return stateItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item => JsonSerializer.Deserialize<V2NewsCategory>(item.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                .Where(cat => cat != null)
                .ToList()!;
        }

        public async Task<V2NewsCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var key = GetCategoryKey(id);
            return await _dapr.GetStateAsync<V2NewsCategory>(V2Content.ContentStoreName, key, cancellationToken: cancellationToken);
        }

        private static int _nextCategoryId = 101;
        private static int _nextSubCategoryId = 1001;

        private static string GetCategoryKey(int id) => $"category-{id}";

        public async Task AddCategoryAsync(V2NewsCategory category, CancellationToken cancellationToken = default)
        {
            category.Id = category.Id == 0 ? _nextCategoryId++ : category.Id;
            category.SubCategories ??= new();

            foreach (var sub in category.SubCategories)
            {
                sub.Id = sub.Id == 0 ? _nextSubCategoryId++ : sub.Id;
            }

            var key = GetCategoryKey(category.Id);
            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, category, cancellationToken: cancellationToken);

            var index = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.NewsCategoryIndexKey, cancellationToken: cancellationToken) ?? new();
            if (!index.Contains(key))
            {
                index.Add(key);
                await _dapr.SaveStateAsync(V2Content.ContentStoreName, V2Content.NewsCategoryIndexKey, index, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Category {Id} saved to Redis", category.Id);
        }

        public async Task<bool> UpdateSubCategoryAsync(int categoryId, V2NewsSubCategory updatedSubCategory, CancellationToken cancellationToken = default)
        {
            var key = GetCategoryKey(categoryId);
            var category = await _dapr.GetStateAsync<V2NewsCategory>(V2Content.ContentStoreName, key, cancellationToken: cancellationToken);

            if (category == null || category.SubCategories == null)
                return false;

            var existing = category.SubCategories.FirstOrDefault(s => s.Id == updatedSubCategory.Id);
            if (existing == null)
                return false;

            existing.SubCategoryName = updatedSubCategory.SubCategoryName;
            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, category, cancellationToken: cancellationToken);
            return true;
        }
    }
}


