using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text.Json;
using System.Text.RegularExpressions;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.NewsInternalService
{
    public class V2InternalNewsService : IV2NewsService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<IV2NewsService> _logger;
        private const string StoreName = V2Content.ContentStoreName;
        private const string DailyStore = ConstantValues.V2Content.ContentStoreName;
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
            var slug = title.ToLowerInvariant().Trim();
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');
            return slug;
        }
        public async Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {


                var duplicateCheck = dto.Categories.GroupBy(c => new { c.CategoryId, c.SubcategoryId }).
                    Where(d => d.Count() > 1)
                    .Select(d => d.Key).ToList();
                if (duplicateCheck.Any())
                {
                    var duplicates = string.Join(", ",  duplicateCheck.Select(h=> $"CategoryId:{h.CategoryId}, subCategoryId:{h.SubcategoryId}"));
                    throw new InvalidDataException($"Please select different category and subcategory combinations. Duplicates: {duplicates}");

                }

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
                        PublishedDate = cat.SlotId == (int)Slot.UnPublished ? null : DateTime.UtcNow,
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

        private async Task<List<Guid>> GetAllDailyTopicIdsAsync(CancellationToken ct)
        {
            try
            {
                var topics = await _dapr.GetStateAsync<List<Guid>>(
                    DailyStore,
                    "daily-topics-index",
                    cancellationToken: ct)
                    ?? new List<Guid>();
                return topics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read daily-topics-index");
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }
        }

        private async Task<List<DailyTopicContent>> GetSlotsByTopicIdAsync(Guid topicId, CancellationToken ct)
        {
            try
            {
                var key = $"daily-topic-{topicId}-slots";
                var slots = await _dapr.GetStateAsync<List<DailyTopicContent>>(
                    DailyStore,
                    key,
                    cancellationToken: ct)
                    ?? new List<DailyTopicContent>();
                return slots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read slots for topic {TopicId}", topicId);
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }
        }

        public async Task<string> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            try
            {
                var topSlotTasks = Enumerable.Range(1, 9)
                    .Select(i => _dapr.GetStateAsync<DailyTopSectionSlot>(
                        DailyStore,
                        $"daily-slot-{i}",
                        cancellationToken: cancellationToken))
                    .ToArray();

                var topSlots = (await Task.WhenAll(topSlotTasks))
                    .Where(s => s != null)
                    .ToList();

                var usedInTop = topSlots
                    .FirstOrDefault(s => s.ContentType == DailyContentType.Article
                                      && s.RelatedContentId == id);

                if (usedInTop != null)
                {
                    throw new InvalidOperationException(
                        $"Cannot delete news {id}: it’s used in Daily Top Section slot #{usedInTop.SlotNumber}");
                }
            }
            catch (DaprServiceException)
            {
                throw;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error reading Daily Top-Section slots before delete");
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }

            List<Guid> topicIds = await GetAllDailyTopicIdsAsync(cancellationToken);
            foreach (var topicId in topicIds)
            {
                var topicSlots = await GetSlotsByTopicIdAsync(topicId, cancellationToken);
                var usedInTopic = topicSlots
                    .FirstOrDefault(ts => ts.ContentType == DailyContentType.Article
                                       && ts.RelatedContentId == id);

                if (usedInTopic != null)
                {
                    throw new InvalidOperationException(
                        $"Cannot delete news {id}: it’s used in Topic '{topicId}' slot #{usedInTopic.SlotNumber}");
                }
            }

            var existing = await _dapr.GetStateAsync<V2NewsArticleDTO>(
                StoreName,
                key,
                cancellationToken: cancellationToken);

            if (existing == null)
            {
                throw new KeyNotFoundException($"News with ID '{id}' not found.");
            }

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                StoreName,
                key,
                existing,
                new StateOptions { Consistency = ConsistencyMode.Strong },
                cancellationToken: cancellationToken);

            var index = await _dapr.GetStateAsync<List<string>>(
                StoreName,
                V2Content.NewsIndexKey,
                cancellationToken: cancellationToken)
                ?? new List<string>();

            if (index.Remove(key))
            {
                await _dapr.SaveStateAsync(
                    StoreName,
                    V2Content.NewsIndexKey,
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
            var articleId = dto.Id;

            try
            {
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

                // BLOCK UNPUBLISHING IF USED IN DAILY SECTIONS
                if (newSlot == UnpublishedId)
                {
                    // Check Daily Top Slots
                    var topSlotTasks = Enumerable.Range(1, 9)
                        .Select(i => _dapr.GetStateAsync<DailyTopSectionSlot>(
                            DailyStore,
                            $"daily-slot-{i}",
                            cancellationToken: cancellationToken))
                        .ToArray();

                    var topSlots = (await Task.WhenAll(topSlotTasks))
                        .Where(s => s != null)
                        .ToList();

                    foreach (var slot in topSlots)
                    {
                        _logger.LogInformation("Top Slot#{SlotNumber}: Type={ContentType}, RelatedId={RelatedId}",
                            slot.SlotNumber, slot.ContentType, slot.RelatedContentId);
                    }

                    var usedInTop = topSlots
                        .FirstOrDefault(s => s.ContentType == DailyContentType.Article
                                          && s.RelatedContentId == dto.Id);

                    if (usedInTop != null)
                    {
                        _logger.LogWarning("Cannot unpublish news article {ArticleId}: It’s used in Daily Top Section slot #{SlotNumber}",
                            dto.Id, usedInTop.SlotNumber);

                        throw new InvalidOperationException(
                            $"Cannot unpublish news {dto.Id}: it’s used in Daily Top Section slot #{usedInTop.SlotNumber}");
                    }

                    // Check Topic Slots
                    var topicIds = await GetAllDailyTopicIdsAsync(cancellationToken);
                    foreach (var topicId in topicIds)
                    {
                        var topicSlots = await GetSlotsByTopicIdAsync(topicId, cancellationToken);

                        foreach (var slot in topicSlots)
                        {
                            _logger.LogInformation("Topic {TopicId} Slot#{SlotNumber}: Type={ContentType}, RelatedId={RelatedId}",
                                topicId, slot.SlotNumber, slot.ContentType, slot.RelatedContentId);
                        }

                        var usedInTopic = topicSlots
                            .FirstOrDefault(ts => ts.ContentType == DailyContentType.Article
                                               && ts.RelatedContentId == dto.Id);

                        if (usedInTopic != null)
                        {
                            _logger.LogWarning("Cannot unpublish news article {ArticleId}: It’s used in Topic {TopicId}, Slot #{SlotNumber}",
                                dto.Id, topicId, usedInTopic.SlotNumber);

                            throw new InvalidOperationException(
                                $"Cannot unpublish news {dto.Id}: it’s used in Topic '{topicId}' slot #{usedInTopic.SlotNumber}");
                        }
                    }

                    dto.PublishedDate = null; // Only if safe to unpublish
                }
                else if (oldSlot == UnpublishedId && (newSlot >= 1 && newSlot <= MaxLiveSlot))
                {
                    dto.PublishedDate = DateTime.UtcNow;
                }

                // Handle Slot Change
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
                        var oldSlotKey = GetSlotKey(oldCat.CategoryId, oldCat.SubcategoryId, oldSlot);
                        await _dapr.DeleteStateAsync(store, oldSlotKey, cancellationToken: cancellationToken);
                    }

                    if (oldSlot == UnpublishedId || oldSlot == PublishedId)
                    {
                        var oldStatusKey = GetStatusSlotKey(oldCat.CategoryId, oldCat.SubcategoryId, oldSlot);
                        await _dapr.DeleteStateAsync(store, oldStatusKey, cancellationToken: cancellationToken);
                    }

                    var statusKey = GetStatusSlotKey(newCat.CategoryId, newCat.SubcategoryId, newSlot);
                    await _dapr.SaveStateAsync(store, statusKey, dto.Id.ToString(), cancellationToken: cancellationToken);
                }

                dto.UpdatedAt = DateTime.UtcNow;
                await _dapr.SaveStateAsync(store, key, dto, cancellationToken: cancellationToken);

                _logger.LogInformation("News article {ArticleId} updated successfully.", dto.Id);

                return "News article updated successfully.";
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Validation failed while updating news article {ArticleId}", articleId);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Blocked update for news article {ArticleId} due to slot constraint", articleId);
                throw;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "News article not found for update: {ArticleId}", articleId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating news article {ArticleId}", articleId);
                throw new Exception("Something went wrong while updating the news article.", ex);
            }
        }


        public async Task<string> ReorderSlotsAsync(
            NewsSlotReorderRequest request,
            CancellationToken cancellationToken = default)
        {
            const int MaxSlot = 13;
            string storeName = V2Content.ContentStoreName;
            int catId = request.CategoryId;
            int subCatId = request.SubCategoryId;

            if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

            var slots = request.SlotAssignments.Select(x => x.SlotNumber).ToList();
            if (slots.Distinct().Count() != MaxSlot || slots.Any(n => n < 1 || n > MaxSlot))
                throw new InvalidDataException($"SlotNumber must be unique and between 1 and {MaxSlot}.");

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

                if (!article.Categories.Any(c =>
                      c.CategoryId == catId &&
                      c.SubcategoryId == subCatId))
                {
                    throw new InvalidOperationException(
                        $"Article '{sa.ArticleId}' does not belong to Category {catId} / SubCategory {subCatId}.");
                }

                loaded[sa.ArticleId] = article;
            }


            foreach (var sa in request.SlotAssignments)
            {
                string slotKey = GetSlotKey(catId, subCatId, sa.SlotNumber);

                if (string.IsNullOrWhiteSpace(sa.ArticleId))
                {

                    await _dapr.DeleteStateAsync(storeName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var article = loaded[sa.ArticleId]!;


                var catDto = article.Categories
                    .First(c => c.CategoryId == catId && c.SubcategoryId == subCatId);
                catDto.SlotId = sa.SlotNumber;

                // save both keyed slot and by‐ID
                await _dapr.SaveStateAsync(storeName, slotKey, article, cancellationToken: cancellationToken);
                await _dapr.SaveStateAsync(storeName, article.Id.ToString(), article, cancellationToken: cancellationToken);
            }

            return "News slots updated successfully.";
        }
        public async Task<V2NewsArticleDTO?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    V2Content.ContentStoreName,
                    V2Content.NewsIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                if (keys.Count == 0)
                    return null;

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

                    if (article is not null && article.IsActive && article.Id == id)
                    {
                        return article;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
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
                Console.WriteLine($"[INFO] Fetching comments for article ID: {nid}");

                var indexKey = $"{V2Content.NewsCommentIndexPrefix}{nid}";
                Console.WriteLine($"[INFO] Index key: {indexKey}");

                var index = await _dapr.GetStateAsync<List<Guid>>(V2Content.ContentStoreName, indexKey, cancellationToken: ct)
                                 ?? new List<Guid>();

                Console.WriteLine($"[INFO] Comment count in index: {index.Count}");

                // If there are no comments, return a 404
                if (index.Count == 0)
                {
                    Console.WriteLine($"[INFO] No comments found for article {nid}");
                    throw new KeyNotFoundException($"No comments found for article {nid}");
                }

                int currentPage = page ?? 1;
                int itemsPerPage = perPage ?? 10;
                int skip = (currentPage - 1) * itemsPerPage;

                var pagedCommentIds = index.Skip(skip).Take(itemsPerPage).ToList();
                Console.WriteLine($"[INFO] Paged comment IDs: {string.Join(", ", pagedCommentIds)}");

                var commentKeys = pagedCommentIds
                    .Select(id => $"{V2Content.NewsCommentPrefix}-{nid}-{id}")
                    .ToList();

                Console.WriteLine($"[INFO] Comment keys: {string.Join(", ", commentKeys)}");

                var commentStates = await _dapr.GetBulkStateAsync(
                    storeName: V2Content.ContentStoreName,
                    keys: commentKeys,
                    parallelism: null,
                    metadata: null,
                    cancellationToken: ct
                );

                var allComments = new List<V2NewsCommentDto>();

                foreach (var state in commentStates)
                {
                    if (string.IsNullOrWhiteSpace(state.Value))
                    {
                        Console.WriteLine($"[WARN] Empty value for key: {state.Key}");
                        continue;
                    }

                    try
                    {
                        var comment = JsonSerializer.Deserialize<V2NewsCommentDto>(state.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (comment != null && comment.IsActive && comment.CommentId != Guid.Empty)
                        {
                            allComments.Add(comment);
                            Console.WriteLine($"[INFO] Loaded comment {comment.CommentId}, parent: {comment.ParentCommentId}");
                        }
                        else
                        {
                            Console.WriteLine($"[INFO] Skipped invalid or inactive comment: {state.Key}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to deserialize comment for key: {state.Key}, ex: {ex.Message}");
                    }
                }

                // If no comments were loaded
                if (!allComments.Any())
                {
                    Console.WriteLine($"[INFO] No valid comments found for article {nid}");
                    throw new KeyNotFoundException($"No valid comments found for article {nid}");
                }

                var grouped = new Dictionary<Guid, List<V2NewsCommentDto>>();

                foreach (var comment in allComments.Where(c => c.CommentId != Guid.Empty))
                {
                    var parentId = comment.ParentCommentId ?? Guid.Empty;

                    if (!grouped.ContainsKey(parentId))
                        grouped[parentId] = new List<V2NewsCommentDto>();

                    grouped[parentId].Add(comment);
                }

                var topLevel = grouped.ContainsKey(Guid.Empty)
                    ? grouped[Guid.Empty].OrderByDescending(c => c.CommentedAt).ToList()
                    : new List<V2NewsCommentDto>();

                var comments = new List<NewsCommentListItem>();

                foreach (var parent in topLevel)
                {
                    var likeIndexKey = $"news-comment-like-index-{parent.CommentId}";

                    var likes = await _dapr.GetStateAsync<List<ReactionUser>>(V2Content.ContentStoreName, likeIndexKey, cancellationToken: ct) ?? new();

                    var commentItem = new NewsCommentListItem
                    {
                        CommentId = parent.CommentId,
                        UserId = parent.Uid ?? "",
                        UserName = parent.UserName ?? "",
                        Subject = parent.Comment,
                        DateCreated = parent.CommentedAt,
                        LikeCount = likes.Count,
                        LikedUsers = likes.Select(u => new UserSummary { UserId = u.UserId, UserName = u.UserName }).ToList(),
                        Replies = new List<NewsCommentListItem>()
                    };

                    if (grouped.ContainsKey(parent.CommentId))
                    {
                        foreach (var reply in grouped[parent.CommentId])
                        {
                            var replyLikeKey = $"news-comment-like-index-{reply.CommentId}";

                            var replyLikes = await _dapr.GetStateAsync<List<ReactionUser>>(V2Content.ContentStoreName, replyLikeKey, cancellationToken: ct) ?? new();

                            commentItem.Replies.Add(new NewsCommentListItem
                            {
                                CommentId = reply.CommentId,
                                UserId = reply.Uid ?? "",
                                UserName = reply.UserName ?? "",
                                Subject = reply.Comment,
                                DateCreated = reply.CommentedAt,
                                LikeCount = replyLikes.Count,
                                LikedUsers = replyLikes.Select(u => new UserSummary { UserId = u.UserId, UserName = u.UserName }).ToList(),
                            });

                            Console.WriteLine($"[INFO] Added reply {reply.CommentId} to parent {parent.CommentId}");
                        }
                    }

                    comments.Add(commentItem);
                }

                Console.WriteLine($"[INFO] Total top-level comments returned: {comments.Count}");

                return new NewsCommentListResponse
                {
                    TotalComments = comments.Count,
                    PerPage = itemsPerPage,
                    CurrentPage = currentPage,
                    Comments = comments
                };
            }
            catch (KeyNotFoundException knfEx)
            {
                Console.WriteLine($"[INFO] No comments found: {knfEx.Message}");
                throw new InvalidOperationException("No comments found for the provided article ID.", knfEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get comments for article {nid}: {ex}");
                throw new InvalidOperationException("Error fetching comments", ex);
            }
        }

        public async Task<bool> LikeNewsCommentAsync(string commentId, string userId, string userName, CancellationToken ct = default)
        {
            var key = $"news-comment-like-{commentId}-{userId}";
            var indexKey = $"news-comment-like-index-{commentId}";

            try
            {
                var existing = await _dapr.GetStateAsync<string>(StoreName, key, cancellationToken: ct);
                var index = await _dapr.GetStateAsync<List<ReactionUser>>(StoreName, indexKey, cancellationToken: ct) ?? new();

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    await _dapr.DeleteStateAsync(StoreName, key, cancellationToken: ct);
                    index.RemoveAll(u => u.UserId == userId);
                    await _dapr.SaveStateAsync(StoreName, indexKey, index, cancellationToken: ct);

                    _logger.LogInformation("User {UserId} unliked comment {CommentId}", userId, commentId);
                    return false;
                }

                await _dapr.SaveStateAsync(StoreName, key, userId, cancellationToken: ct);

                if (!index.Any(u => u.UserId == userId))
                    index.Add(new ReactionUser { UserId = userId, UserName = userName });


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

        public async Task<NewsCommentApiResponse> SoftDeleteNewsCommentAsync(string articleId, Guid commentId, string userId, CancellationToken ct = default)
        {
            try
            {
                var commentKey = $"{V2Content.NewsCommentPrefix}-{articleId}-{commentId}";

                Console.WriteLine($"[INFO] Attempting to delete comment key: {commentKey}");


                var comment = await _dapr.GetStateAsync<V2NewsCommentDto>(V2Content.ContentStoreName, commentKey, cancellationToken: ct);

                if (comment == null)
                {
                    Console.WriteLine($"[WARN] Comment not found or failed to deserialize for key: {commentKey}");

                    return new NewsCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Comment not found or invalid"
                    };
                }

                if (!comment.IsActive)
                {
                    return new NewsCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Comment already deleted"
                    };
                }

                if (!string.Equals(comment.Uid, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return new NewsCommentApiResponse
                    {
                        Status = "failed",
                        Message = "You are not authorized to delete this comment"
                    };
                }

                comment.IsActive = false;

                await _dapr.SaveStateAsync(V2Content.ContentStoreName, commentKey, comment, cancellationToken: ct);

                Console.WriteLine($"[INFO] Soft-deleted comment ID: {commentId}");

                return new NewsCommentApiResponse
                {
                    Status = "success",
                    Message = "Comment deleted successfully"
                };
            }

            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception while deleting comment {commentId}: {ex.Message}");

                return new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Error occurred while deleting comment"
                };
            }
        }

        public async Task<Common.Infrastructure.DTO_s.GenericNewsPageResponse> GetNewsLandingPageAsync(
      int categoryId,
      int subCategoryId,
      CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("GetNewsLandingPageAsync called with CategoryId={CategoryId}, SubCategoryId={SubCategoryId}", categoryId, subCategoryId);

                var categoryDto = await GetCategoryByIdAsync(categoryId, cancellationToken)
                                    ?? throw new KeyNotFoundException($"Category {categoryId} not found");

                _logger.LogInformation("Category found: {CategoryName}", categoryDto.CategoryName);

                var catKey = categoryDto.CategoryName.ToLowerInvariant().Replace(" ", "_");

                var subDto = categoryDto.SubCategories.FirstOrDefault(s => s.Id == subCategoryId)
                            ?? throw new KeyNotFoundException($"SubCategory {subCategoryId} not found");

                _logger.LogInformation("SubCategory found: {SubCategoryName}", subDto.SubCategoryName);

                var subKey = subDto.SubCategoryName.ToLowerInvariant().Replace(" ", "_");
                var pageName = $"qln_{catKey}_{subKey}";

                var dtos = await GetArticlesBySubCategoryIdAsync(categoryId, subCategoryId, cancellationToken);
                _logger.LogInformation("Fetched {Count} articles for CategoryId={CategoryId} and SubCategoryId={SubCategoryId}", dtos.Count, categoryId, subCategoryId);

                var articlesInSlot1to4 = dtos
                    .Where(dto => dto.Categories.Any(c => c.SlotId >= 1 && c.SlotId <= 4))
                    .OrderBy(dto => dto.Categories.First(c => c.SlotId >= 1 && c.SlotId <= 4).SlotId)
                    .ToList();
                _logger.LogInformation("Slot 1–4 Articles: {Count}", articlesInSlot1to4.Count);

                var articlesInSlot5to8 = dtos
                    .Where(dto => dto.Categories.Any(c => c.SlotId >= 5 && c.SlotId <= 8))
                    .OrderBy(dto => dto.Categories.First(c => c.SlotId >= 5 && c.SlotId <= 8).SlotId)
                    .ToList();
                _logger.LogInformation("Slot 5–8 Articles: {Count}", articlesInSlot5to8.Count);

                var articlesInSlot9to13 = dtos
                    .Where(dto => dto.Categories.Any(c => c.SlotId >= 9 && c.SlotId <= 13))
                    .OrderBy(dto => dto.Categories.First(c => c.SlotId >= 9 && c.SlotId <= 13).SlotId)
                    .ToList();
                _logger.LogInformation("Slot 9–13 Articles: {Count}", articlesInSlot9to13.Count);

                var articlesInSlot14 = dtos
                    .Where(dto => dto.Categories.Any(c => c.SlotId == 14))
                    .OrderByDescending(dto => dto.PublishedDate)
                    .ToList();
                _logger.LogInformation("Slot 14 Articles Total: {Count}", articlesInSlot14.Count);

                var top4Slot14 = articlesInSlot14.Take(4).ToList();
                var remainingSlot14 = articlesInSlot14.Skip(4).ToList();
                _logger.LogInformation("Slot 14 - Top4: {TopCount}, Remaining: {RemainingCount}", top4Slot14.Count, remainingSlot14.Count);

                List<Common.Infrastructure.DTO_s.ContentPost> MapToPosts(List<V2NewsArticleDTO> list)
                {
                    var posts = new List<Common.Infrastructure.DTO_s.ContentPost>();
                    foreach (var dto in list)
                    {
                        try
                        {
                            posts.Add(new Common.Infrastructure.DTO_s.ContentPost
                            {
                                Id = dto.Id,
                                Nid = dto.Id.ToString(),
                                DateCreated = dto.CreatedAt.ToString("o"),
                                ImageUrl = dto.CoverImageUrl,
                                UserName = dto.authorName,
                                Title = dto.Title,
                                Description = dto.Content,
                                Category = categoryDto.CategoryName,
                                NodeType = "post",
                                IsActive = dto.IsActive,
                                Slug = dto.Slug,
                                CreatedAt = dto.CreatedAt,
                                UpdatedAt = dto.UpdatedAt
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error mapping article Id={Id}", dto.Id);
                        }
                    }
                    return posts;
                }

                Common.Infrastructure.DTO_s.BaseQueueResponse<Common.Infrastructure.DTO_s.ContentPost> BuildQueue(string sectionKey, string label, IEnumerable<Common.Infrastructure.DTO_s.ContentPost> items)
                {
                    var qName = $"{pageName}_{sectionKey}";
                    var list = items.ToList();
                    list.ForEach(p =>
                    {
                        p.PageName = pageName;
                        p.QueueName = qName;
                        p.QueueLabel = label;
                    });

                    return new Common.Infrastructure.DTO_s.BaseQueueResponse<Common.Infrastructure.DTO_s.ContentPost>
                    {
                        QueueLabel = label,
                        Items = list
                    };
                }

                var qlnPage = new Common.Infrastructure.DTO_s.GenericNewsPage
                {
                    TopStory = BuildQueue("top_story", "Top Story", MapToPosts(articlesInSlot1to4)),
                    MoreArticles = BuildQueue("more_articles", "More Articles", MapToPosts(articlesInSlot5to8)),
                    Articles1 = BuildQueue("articles_1", "Articles 1", MapToPosts(articlesInSlot9to13)),
                    Articles2 = BuildQueue("articles_2", "Articles 2", MapToPosts(remainingSlot14)),
                    MostPopularArticles = BuildQueue("most_popular_articles", "Most Popular Articles", MapToPosts(top4Slot14)),
                    WatchOnQatarLiving = new Common.Infrastructure.DTO_s.BaseQueueResponse<Common.Infrastructure.DTO_s.ContentVideo>
                    {
                        QueueLabel = "Watch on Qatar Living",
                        Items = Enumerable.Empty<Common.Infrastructure.DTO_s.ContentVideo>().ToList()
                    }
                };

                _logger.LogInformation("News page successfully built for PageName: {PageName}", pageName);

                return new Common.Infrastructure.DTO_s.GenericNewsPageResponse { News = qlnPage };
            }
            catch (KeyNotFoundException knf)
            {
                _logger.LogWarning(knf, "GetNewsLandingPageAsync: not found Category={CategoryId} or SubCategory={SubCategoryId}", categoryId, subCategoryId);
                throw;
            }
            catch (ArgumentException ae)
            {
                _logger.LogWarning(ae, "GetNewsLandingPageAsync: bad arguments Category={CategoryId} or SubCategory={SubCategoryId}", categoryId, subCategoryId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetNewsLandingPageAsync: unexpected error for Category={CategoryId}, SubCategory={SubCategoryId}", categoryId, subCategoryId);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);

                throw new ApplicationException("An error occurred while retrieving the news landing page. See inner exception for details.", ex);
            }
        }



        public async Task<NewsCommentApiResponse> EditNewsCommentAsync(string articleId, Guid commentId, string userId, string updatedText, CancellationToken ct = default)
        {
            try
            {
                var commentKey = $"{V2Content.NewsCommentPrefix}-{articleId}-{commentId}";
                Console.WriteLine($"[INFO] Attempting to edit comment key: {commentKey}");

                var comment = await _dapr.GetStateAsync<V2NewsCommentDto>(V2Content.ContentStoreName, commentKey, cancellationToken: ct);

                if (comment == null)
                {
                    Console.WriteLine($"[WARN] Comment not found or invalid for key: {commentKey}");
                    return new NewsCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Comment not found"
                    };
                }

                if (!comment.IsActive)
                {
                    return new NewsCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Cannot edit a deleted comment"
                    };
                }

                if (!string.Equals(comment.Uid, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return new NewsCommentApiResponse
                    {
                        Status = "failed",
                        Message = "You are not authorized to edit this comment"
                    };
                }

                comment.Comment = updatedText;
                comment.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(V2Content.ContentStoreName, commentKey, comment, cancellationToken: ct);
                Console.WriteLine($"[INFO] Edited comment ID: {commentId}");

                return new NewsCommentApiResponse
                {
                    Status = "success",
                    Message = "Comment updated successfully"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception while editing comment {commentId}: {ex.Message}");

                return new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Error occurred while editing comment"
                };
            }
        }
    }
}



