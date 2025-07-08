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
        private const string StoreName = V2Content.ContentStoreName;
        private static readonly List<string> writerTags = new()
    {
        "Qatar Living",
        "Everything Qatar",
        "FIFA Arab Cup",
        "QL Exclusive",
        "Advice & Help"
    };
        private static int _nextCategoryId = 101;
        private static int _nextSubCategoryId = 1001;

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
                var slugBase = GenerateNewsSlug(dto.Title);
                int articleCount = 0;

                foreach (var cat in dto.Categories)
                {
                    var singleDto = new V2NewsArticleDTO
                    {
                        Id = Guid.NewGuid(),
                        Title = dto.Title,
                        Content = dto.Content,
                        WriterTag = dto.WriterTag,
                        Slug = $"{slugBase}-{cat.CategoryId}-{cat.SubcategoryId}",
                        IsActive = true,
                        Categories = new List<V2ArticleCategory> {
            new V2ArticleCategory {
                CategoryId    = cat.CategoryId,
                SubcategoryId = cat.SubcategoryId,
                SlotId        = cat.SlotId == 0
                                  ? (int)Slot.UnPublished
                                  : cat.SlotId
            }
        },
                        PublishedDate = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        authorName = dto.authorName,
                        CoverImageUrl = dto.CoverImageUrl,
                        UserId = dto.UserId
                    };

                    int slotId = singleDto.Categories[0].SlotId;
                    string storeName = V2Content.ContentStoreName;
                    string articleIdStr = singleDto.Id.ToString();

                    if (slotId > 0)
                    {
                        await HandleSlotShiftAsync(
                            cat.CategoryId,
                            cat.SubcategoryId,
                            slotId,
                            singleDto,
                            cancellationToken);
                    }
                    else
                    {
                        string statusKey = GetStatusSlotKey(
                            cat.CategoryId,
                            cat.SubcategoryId,
                            slotId);
                        await _dapr.SaveStateAsync(
                            storeName,
                            statusKey,
                            articleIdStr,
                            cancellationToken: cancellationToken);
                    }

                    await _dapr.SaveStateAsync(
                        storeName,
                        articleIdStr,
                        singleDto,
                        cancellationToken: cancellationToken);

                    var indexKey = V2Content.NewsIndexKey;
                    var currentIndex = await _dapr.GetStateAsync<List<string>>(
                        storeName,
                        indexKey,
                        cancellationToken: cancellationToken)
                       ?? new List<string>();

                    if (!currentIndex.Contains(articleIdStr))
                    {
                        currentIndex.Add(articleIdStr);
                        await _dapr.SaveStateAsync(
                            storeName,
                            indexKey,
                            currentIndex,
                            cancellationToken: cancellationToken);
                    }

                    articleCount++;
                }


                return $"{articleCount} news article(s) created successfully";
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create article(s)");
                throw new Exception("Unexpected error during article creation", ex);
            }
        }



        private async Task<string> HandleSlotShiftAsync(int categoryId, int subCategoryId, int desiredSlot, V2NewsArticleDTO newArticle, CancellationToken cancellationToken)
        {
            const int MaxSlot = 13;
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
            var key = id.ToString();

            // Fetch existing article
            var existing = await _dapr.GetStateAsync<V2NewsArticleDTO>(
                ConstantValues.V2Content.ContentStoreName,
                key,
                cancellationToken: cancellationToken);

            if (existing == null)
                throw new KeyNotFoundException($"News with ID '{id}' not found.");

            // Mark as inactive (soft delete)
            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            // Save back to store
            await _dapr.SaveStateAsync(
                ConstantValues.V2Content.ContentStoreName,
                key,
                existing,
                new StateOptions { Consistency = ConsistencyMode.Strong },
                cancellationToken: cancellationToken);

            // --- 🔥 Remove ID from News Index ---
            var index = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.V2Content.ContentStoreName,
                ConstantValues.V2Content.NewsIndexKey,
                cancellationToken: cancellationToken) ?? new();

            if (index.Contains(key))
            {
                index.Remove(key);
                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.NewsIndexKey,
                    index,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Removed article ID {NewsId} from index after soft delete", id);
            }

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

        public async Task<string> UpdateNewsArticleAsync(
            V2NewsArticleDTO dto,
            CancellationToken cancellationToken = default)
        {
            const int MaxLiveSlot = 13;
            const int UnpublishedId = (int)Slot.UnPublished;
            const int PublishedId = (int)Slot.Published;

            var store = V2Content.ContentStoreName;
            if (dto.Id == Guid.Empty)
                throw new InvalidDataException("Id is required for update");

            var key = dto.Id.ToString();
            var existing = await _dapr.GetStateAsync<V2NewsArticleDTO>(
                store, key, cancellationToken: cancellationToken);

            if (existing == null)
                throw new KeyNotFoundException("News article not found");

            var oldCat = existing.Categories.First();
            var oldSlot = oldCat.SlotId;

            var newCat = dto.Categories.First();
            var newSlot = newCat.SlotId;

            if (newSlot >= 1 && newSlot <= MaxLiveSlot)
            {
                await HandleSlotShiftAsync(
                    newCat.CategoryId,
                    newCat.SubcategoryId,
                    newSlot,
                    dto,
                    cancellationToken);
            }
            else
            {
                if (oldSlot >= 1 && oldSlot <= MaxLiveSlot)
                {
                    var oldSlotKey = GetSlotKey(
                        oldCat.CategoryId,
                        oldCat.SubcategoryId,
                        oldSlot);
                    await _dapr.DeleteStateAsync(
                        store,
                        oldSlotKey,
                        cancellationToken: cancellationToken);
                }

                if (oldSlot == UnpublishedId || oldSlot == PublishedId)
                {
                    var oldStatusKey = GetStatusSlotKey(
                        oldCat.CategoryId,
                        oldCat.SubcategoryId,
                        oldSlot);
                    await _dapr.DeleteStateAsync(
                        store,
                        oldStatusKey,
                        cancellationToken: cancellationToken);
                }

                var statusKey = GetStatusSlotKey(
                    newCat.CategoryId,
                    newCat.SubcategoryId,
                    newSlot);
                await _dapr.SaveStateAsync(
                    store,
                    statusKey,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);
            }

            dto.UpdatedAt = DateTime.UtcNow;
            await _dapr.SaveStateAsync(
                store,
                key,
                dto,
                cancellationToken: cancellationToken);

            return "News article updated successfully.";
        }
        public async Task<string> ReorderSlotsAsync(
            NewsSlotReorderRequest request,
            CancellationToken cancellationToken = default)
        {
            const int MaxSlot = 13;
            string storeName = V2Content.ContentStoreName;
            int catId = request.CategoryId;
            int subCatId = request.SubCategoryId;

            // 1) Validate count & slot numbers
            if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

            var slots = request.SlotAssignments.Select(x => x.SlotNumber).ToList();
            if (slots.Distinct().Count() != MaxSlot || slots.Any(n => n < 1 || n > MaxSlot))
                throw new InvalidDataException($"SlotNumber must be unique and between 1 and {MaxSlot}.");

            // 2) Preload all referenced articles
            var loaded = new Dictionary<string, V2NewsArticleDTO>();
            foreach (var sa in request.SlotAssignments)
            {
                if (string.IsNullOrWhiteSpace(sa.ArticleId))
                    continue;

                var article = await _dapr.GetStateAsync<V2NewsArticleDTO>(
                    storeName,
                    sa.ArticleId,
                    cancellationToken: cancellationToken);

                if (article == null)
                    throw new InvalidDataException($"Article with ID '{sa.ArticleId}' not found.");

                // ensure it belongs to this category/subcategory
                if (!article.Categories.Any(c =>
                      c.CategoryId == catId &&
                      c.SubcategoryId == subCatId))
                {
                    throw new InvalidOperationException(
                        $"Article '{sa.ArticleId}' does not belong to Category {catId} / SubCategory {subCatId}.");
                }

                loaded[sa.ArticleId] = article;
            }

            // 3) Write each slot
            foreach (var sa in request.SlotAssignments)
            {
                string slotKey = GetSlotKey(catId, subCatId, sa.SlotNumber);

                if (string.IsNullOrWhiteSpace(sa.ArticleId))
                {
                    // clear this slot
                    await _dapr.DeleteStateAsync(storeName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var article = loaded[sa.ArticleId]!;

                // update the matching Category DTO inside the article (if you track slot there)
                var catDto = article.Categories
                    .First(c => c.CategoryId == catId && c.SubcategoryId == subCatId);
                catDto.SlotId = sa.SlotNumber;

                // save both keyed slot and by‐ID
                await _dapr.SaveStateAsync(storeName, slotKey, article, cancellationToken: cancellationToken);
                await _dapr.SaveStateAsync(storeName, article.Id.ToString(), article, cancellationToken: cancellationToken);
            }

            return "News slots updated successfully.";
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

        public async Task<NewsCommentApiResponse> SaveNewsCommentAsync(V2NewsCommentDto dto, CancellationToken ct = default)
        {
            _logger.LogInformation("Starting to save news comment for Article ID: {Nid}", dto.Nid);
            try
            {
                var commentKey = $"{V2Content.NewsCommentPrefix}-{dto.Nid}-{dto.CommentId}";
                var indexKey = $"{V2Content.NewsCommentIndexPrefix}{dto.Nid}";

                _logger.LogInformation("Generated comment key: {CommentKey}", commentKey);
                _logger.LogInformation("Generated index key: {IndexKey}", indexKey);

                _logger.LogInformation("Saving comment state to Dapr store...");
                await _dapr.SaveStateAsync(V2Content.ContentStoreName, commentKey, dto, cancellationToken: ct);
                _logger.LogInformation("Comment saved to key: {CommentKey}", commentKey);

                _logger.LogInformation("Retrieving existing index list from Dapr...");
                var index = await _dapr.GetStateAsync<List<Guid>>(V2Content.ContentStoreName, indexKey, cancellationToken: ct) ?? new();
                _logger.LogInformation("Retrieved {Count} comment IDs from index.", index.Count);


                if (!index.Contains(dto.CommentId))
                {
                    _logger.LogInformation("Comment ID {CommentId} not found in index. Adding...", dto.CommentId);
                    index.Add(dto.CommentId);

                    _logger.LogInformation("Saving updated index back to Dapr...");
                    await _dapr.SaveStateAsync(V2Content.ContentStoreName, indexKey, index, cancellationToken: ct);
                    _logger.LogInformation("Index saved with {Count} total comment IDs.", index.Count);
                }
                else
                {
                    _logger.LogInformation("Comment ID {CommentId} already exists in index. Skipping update.", dto.CommentId);
                }

                _logger.LogInformation("News comment saved successfully for Article {Nid}", dto.Nid);


                return new NewsCommentApiResponse
                {
                    Status = "success",
                    Message = "Comment saved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save news comment for Article {Nid}", dto.Nid);

                return new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Comment could not be saved"
                };
            }
        }
        public async Task<NewsCommentListResponse> GetCommentsByArticleIdAsync(string nid, int? page = null, int? perPage = null, CancellationToken ct = default)
        {
            try
            {
                var indexKey = $"{V2Content.NewsCommentIndexPrefix}{nid}";
                var index = await _dapr.GetStateAsync<List<Guid>>(V2Content.ContentStoreName, indexKey, cancellationToken: ct)
                             ?? new List<Guid>();

                int total = index.Count;
                int currentPage = page ?? 1;
                int itemsPerPage = perPage ?? 10;
                int skip = (currentPage - 1) * itemsPerPage;

                var pagedCommentIds = index
                    .Skip(skip)
                    .Take(itemsPerPage)
                    .ToList();

                var commentKeys = pagedCommentIds
                    .Select(id => $"{V2Content.NewsCommentPrefix}-{nid}-{id}")
                    .ToList();

                var commentStates = await _dapr.GetBulkStateAsync(
                    V2Content.ContentStoreName,
                    commentKeys,
                    null);

                var comments = new List<NewsCommentListItem>();

                foreach (var state in commentStates)
                {
                    if (string.IsNullOrWhiteSpace(state.Value))
                        continue;

                    var comment = JsonSerializer.Deserialize<V2NewsCommentDto>(state.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (comment == null || !comment.IsActive)
                        continue;

                    var likeIndexKey = $"news-comment-like-index-{comment.CommentId}";
                    var likes = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, likeIndexKey, cancellationToken: ct)
                                ?? new List<string>();

                    comments.Add(new NewsCommentListItem
                    {
                        CommentId = comment.CommentId,
                        UserId = comment.Uid ?? string.Empty,
                        UserName = comment.UserName ?? string.Empty,
                        Subject = comment.Comment,
                        DateCreated = comment.CommentedAt,
                        LikeCount = likes.Count
                    });
                }

                return new NewsCommentListResponse
                {
                    TotalComments = comments.Count,
                    PerPage = itemsPerPage,
                    CurrentPage = currentPage,
                    Comments = comments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get comments for article {Nid}", nid);
                throw new InvalidOperationException("Error fetching comments");
            }
        }

        public async Task<bool> LikeNewsCommentAsync(string commentId, string userId, CancellationToken ct = default)
        {
            var key = $"news-comment-like-{commentId}-{userId}";
            var indexKey = $"news-comment-like-index-{commentId}";

            try
            {
                var existing = await _dapr.GetStateAsync<string>(StoreName, key, cancellationToken: ct);
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey, cancellationToken: ct) ?? new();

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    await _dapr.DeleteStateAsync(StoreName, key, cancellationToken: ct);
                    index.Remove(userId);
                    await _dapr.SaveStateAsync(StoreName, indexKey, index, cancellationToken: ct);

                    _logger.LogInformation("User {UserId} unliked comment {CommentId}", userId, commentId);
                    return false;
                }

                await _dapr.SaveStateAsync(StoreName, key, userId, cancellationToken: ct);

                if (!index.Contains(userId))
                    index.Add(userId);

                await _dapr.SaveStateAsync(StoreName, indexKey, index, cancellationToken: ct);

                _logger.LogInformation("User {UserId} liked comment {CommentId}", userId, commentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for comment {CommentId}", commentId);
                throw;
            }
        }

    }
}


