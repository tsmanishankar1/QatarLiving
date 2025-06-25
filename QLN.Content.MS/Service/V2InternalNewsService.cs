using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
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


        //public async Task<CreateNewsArticleResponseDto> CreateNewsArticleAsync(Guid userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        dto.Id = Guid.NewGuid();
        //        dto.CreatedBy = userId;
        //        dto.UpdatedBy = userId;
        //        dto.CreatedAt = DateTime.UtcNow;
        //        dto.UpdatedAt = DateTime.UtcNow;

        //        string storeName = V2Content.ContentStoreName;
        //        string articleIdStr = dto.Id.ToString();

        //        var response = new CreateNewsArticleResponseDto
        //        {
        //            ArticleId = dto.Id,
        //            Message = "News article created successfully"
        //        };

        //        // Save the main article
        //        await _dapr.SaveStateAsync(storeName, articleIdStr, dto, cancellationToken: cancellationToken);

        //        // Update index key
        //        var indexKey = V2Content.NewsIndexKey;
        //        var currentIndex = await _dapr.GetStateAsync<List<string>>(storeName, indexKey, cancellationToken: cancellationToken)
        //                           ?? new List<string>();

        //        if (!currentIndex.Contains(articleIdStr))
        //        {
        //            currentIndex.Add(articleIdStr);
        //            await _dapr.SaveStateAsync(storeName, indexKey, currentIndex, cancellationToken: cancellationToken);
        //        }

        //        // Process slots
        //        foreach (var cat in dto.Categories)
        //        {
        //            int slotId = cat.SlotId == 0 ? (int)Slot.UnPublished : cat.SlotId;

        //            if (slotId >= 1 && slotId <= 13)
        //            {
        //                await HandleSlotShiftAsync(cat.CategoryId, cat.SubcategoryId, slotId, dto.Id, dto, response, cancellationToken);
        //            }
        //            else if (slotId == (int)Slot.Published || slotId == (int)Slot.UnPublished)
        //            {
        //                string key = GetStatusSlotKey(cat.CategoryId, cat.SubcategoryId, slotId);
        //                await _dapr.SaveStateAsync(storeName, key, articleIdStr, cancellationToken: cancellationToken);

        //                response.AssignedSlots.Add(new AssignedSlotDto
        //                {
        //                    CategoryId = cat.CategoryId,
        //                    SubCategoryId = cat.SubcategoryId,
        //                    Slot = ((Slot)slotId).ToString()
        //                });
        //            }
        //            else
        //            {
        //                throw new InvalidDataException($"Invalid SlotId: {slotId} for category {cat.CategoryId}-{cat.SubcategoryId}");
        //            }
        //        }

        //        return response;
        //    }
        //    catch (InvalidDataException ex)
        //    {
        //        _logger.LogError(ex, "Validation error while creating article");
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to create article");
        //        throw new Exception("Unexpected error during article creation", ex);
        //    }
        //}

        //private async Task HandleSlotShiftAsync(
        // int categoryId,
        // int subCategoryId,
        // int desiredSlot,
        // Guid newArticleId,
        // V2NewsArticleDTO originalDto,
        // CreateNewsArticleResponseDto response,
        // CancellationToken cancellationToken)
        //{
        //    const int MaxSlot = 13;
        //    string storeName = V2Content.ContentStoreName;

        //    // Prepare slot keys and retrieve current slot state
        //    var keys = Enumerable.Range(1, MaxSlot)
        //        .Select(i => GetSlotKey(categoryId, subCategoryId, i))
        //        .ToList();

        //    var existingStates = await _dapr.GetBulkStateAsync(
        //        storeName,
        //        keys,
        //        parallelism: null,
        //        metadata: null,
        //        cancellationToken: cancellationToken
        //    );

        //    var slotMap = new Dictionary<int, V2NewsArticleDTO>();
        //    foreach (var item in existingStates)
        //    {
        //        var slotNum = int.Parse(Regex.Match(item.Key, @"slot(\d+)$").Groups[1].Value);
        //        if (!string.IsNullOrWhiteSpace(item.Value))
        //        {
        //            var article = JsonSerializer.Deserialize<V2NewsArticleDTO>(item.Value);
        //            if (article != null && article.Id != Guid.Empty)
        //                slotMap[slotNum] = article;
        //        }
        //    }

        //    // Shift articles from desired slot upwards
        //    for (int i = MaxSlot - 1; i >= desiredSlot; i--)
        //    {
        //        if (slotMap.ContainsKey(i))
        //        {
        //            int nextSlot = i + 1;
        //            if (nextSlot > MaxSlot) continue;

        //            var article = slotMap[i];
        //            var toKey = GetSlotKey(categoryId, subCategoryId, nextSlot);
        //            var fromKey = GetSlotKey(categoryId, subCategoryId, i);

        //            // Update internal SlotId
        //            var cat = article.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
        //            if (cat != null) cat.SlotId = nextSlot;

        //            await _dapr.SaveStateAsync(storeName, toKey, article, cancellationToken: cancellationToken);
        //            await _dapr.DeleteStateAsync(storeName, fromKey, cancellationToken: cancellationToken); // ✅ Remove old slot
        //        }
        //    }

        //    // Save the new article to its final slot
        //    var newSlotKey = GetSlotKey(categoryId, subCategoryId, desiredSlot);
        //    var newArticle = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, newArticleId.ToString(), cancellationToken: cancellationToken)
        //                     ?? originalDto; // ✅ Fallback if not found

        //    if (newArticle != null && newArticle.Id != Guid.Empty)
        //    {
        //        var newCat = newArticle.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
        //        if (newCat != null) newCat.SlotId = desiredSlot;

        //        await _dapr.SaveStateAsync(storeName, newSlotKey, newArticle, cancellationToken: cancellationToken);
        //        await _dapr.SaveStateAsync(storeName, newArticleId.ToString(), newArticle, cancellationToken: cancellationToken); // ✅ Persist updated article

        //        response.AssignedSlots.Add(new AssignedSlotDto
        //        {
        //            CategoryId = categoryId,
        //            SubCategoryId = subCategoryId,
        //            Slot = $"Slot{desiredSlot}"
        //        });
        //    }
        //    else
        //    {
        //        // Log or throw error for invalid article
        //        _logger.LogError("Invalid article detected while shifting slots: {ArticleId}", newArticleId);
        //    }
        //}

        //private string GetSlotKey(int categoryId, int subCategoryId, int slot) =>
        //    $"slot-article-{categoryId}-{subCategoryId}-slot{slot}";

        //private string GetStatusSlotKey(int categoryId, int subCategoryId, int slot) =>
        //    $"slot-article-status-{categoryId}-{subCategoryId}-slot{slot}";
        public async Task<CreateNewsArticleResponseDto> CreateNewsArticleAsync(Guid userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Initialize the article fields
                dto.Id = Guid.NewGuid();
                dto.CreatedBy = userId;
                dto.UpdatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                string storeName = V2Content.ContentStoreName;
                string articleIdStr = dto.Id.ToString();

                var response = new CreateNewsArticleResponseDto
                {
                    ArticleId = dto.Id,
                    Message = "News article created successfully",
                    AssignedSlots = new List<AssignedSlotDto>(),
                    SlotShifts = new List<SlotShiftDto>()
                };

                //ProcessSlotShit(dto);
                // Save the main article
                await _dapr.SaveStateAsync(storeName, articleIdStr, dto, cancellationToken: cancellationToken);

                // Update index key
                var indexKey = V2Content.NewsIndexKey;
                var currentIndex = await _dapr.GetStateAsync<List<string>>(storeName, indexKey, cancellationToken: cancellationToken)
                                    ?? new List<string>();

                if (!currentIndex.Contains(articleIdStr))
                {
                    currentIndex.Add(articleIdStr);
                    await _dapr.SaveStateAsync(storeName, indexKey, currentIndex, cancellationToken: cancellationToken);
                }

                // Process the slots
                foreach (var cat in dto.Categories)
                {
                    int slotId = cat.SlotId == 0 ? (int)Slot.UnPublished : cat.SlotId;

                    // Log the slot key and category
                    _logger.LogInformation("Assigning article to category {categoryId}, subcategory {subcategoryId}, slot {slotId}",
                        cat.CategoryId, cat.SubcategoryId, slotId);

                    if (slotId >= 1 && slotId <= 13)
                    {
                        await HandleSlotShiftAsync(cat.CategoryId, cat.SubcategoryId, slotId, dto.Id, dto, response, cancellationToken);
                    }
                    else if (slotId == (int)Slot.Published || slotId == (int)Slot.UnPublished)
                    {
                        string key = GetStatusSlotKey(cat.CategoryId, cat.SubcategoryId, slotId);
                        await _dapr.SaveStateAsync(storeName, key, articleIdStr, cancellationToken: cancellationToken);

                        response.AssignedSlots.Add(new AssignedSlotDto
                        {
                            CategoryId = cat.CategoryId,
                            SubCategoryId = cat.SubcategoryId,
                            Slot = ((Slot)slotId).ToString()
                        });
                    }
                    else
                    {
                        throw new InvalidDataException($"Invalid SlotId: {slotId} for category {cat.CategoryId}-{cat.SubcategoryId}");
                    }
                }

                return response;
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Validation error while creating article");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create article");
                throw new Exception("Unexpected error during article creation", ex);
            }
        }

        //private void ProcessSlotShit(V2NewsArticleDTO dto)
        //{
        //    foreach (var item in CompanyStoreName.)
        //    {

        //    }
        //}

        private async Task HandleSlotShiftAsync(
      int categoryId,
      int subCategoryId,
      int desiredSlot,
      Guid newArticleId,
      V2NewsArticleDTO originalDto,
      CreateNewsArticleResponseDto response,
      CancellationToken cancellationToken)
        {
            const int MaxSlot = 13;
            string storeName = V2Content.ContentStoreName;

            // Prepare slot keys and retrieve current slot state
            var keys = Enumerable.Range(1, MaxSlot)
                .Select(i => GetSlotKey(categoryId, subCategoryId, i))
                .ToList();

            var existingStates = await _dapr.GetBulkStateAsync(
                storeName,
                keys,
                parallelism: null,
                metadata: null,
                cancellationToken: cancellationToken
            );

            var slotMap = new Dictionary<int, V2NewsArticleDTO>();
            foreach (var item in existingStates)
            {
                var slotNum = int.Parse(Regex.Match(item.Key, @"slot(\d+)$").Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    var article = JsonSerializer.Deserialize<V2NewsArticleDTO>(item.Value);
                    if (article != null)
                        slotMap[slotNum] = article;
                }
            }

            // Shift articles from desired slot upwards
            for (int i = MaxSlot - 1; i >= desiredSlot; i--)
            {
                if (slotMap.ContainsKey(i))
                {
                    int nextSlot = i + 1;
                    if (nextSlot > MaxSlot) continue;

                    var article = slotMap[i];
                    var toKey = GetSlotKey(categoryId, subCategoryId, nextSlot);
                    var fromKey = GetSlotKey(categoryId, subCategoryId, i);

                    // Update internal SlotId
                    var cat = article.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
                    if (cat != null) cat.SlotId = nextSlot;

                    await _dapr.SaveStateAsync(storeName, toKey, article, cancellationToken: cancellationToken);
                    await _dapr.DeleteStateAsync(storeName, fromKey, cancellationToken: cancellationToken);  // Remove old slot

                    response.SlotShifts.Add(new SlotShiftDto
                    {
                        From = $"Slot{i}",
                        To = $"Slot{nextSlot}"
                    });
                }
            }

            // Save the new article to its final slot
            var newSlotKey = GetSlotKey(categoryId, subCategoryId, desiredSlot);
            var newArticle = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, newArticleId.ToString(), cancellationToken: cancellationToken)
                             ?? originalDto;  // Fallback to originalDto if not found

            var newCat = newArticle.Categories.FirstOrDefault(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);
            if (newCat != null) newCat.SlotId = desiredSlot;

            await _dapr.SaveStateAsync(storeName, newSlotKey, newArticle, cancellationToken: cancellationToken);
            await _dapr.SaveStateAsync(storeName, newArticleId.ToString(), newArticle, cancellationToken: cancellationToken);  // Persist updated article

            response.AssignedSlots.Add(new AssignedSlotDto
            {
                CategoryId = categoryId,
                SubCategoryId = subCategoryId,
                Slot = $"Slot{desiredSlot}"
            });
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
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.NewsIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                _logger.LogInformation("Fetched {Count} keys from index", keys.Count);

                var items = await _dapr.GetBulkStateAsync(ConstantValues.V2Content.ContentStoreName, keys, null, cancellationToken: cancellationToken);

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

        public async Task<string> CreateNewsArticleCategoryAsync(V2NewsCategory dto, CancellationToken cancellationToken = default)

        {
            try
            {
                var id = Guid.NewGuid();
                dto.Id = id;
                foreach (var item in dto.SubCategories)
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

                return "News article Category created successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating Category", ex);
            }
        }

        public async Task<List<V2NewsCategory>> GetAllNewsArticleCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2Content.ContentStoreName, ConstantValues.V2Content.NewsIndexKey) ?? new();

                _logger.LogInformation("Fetched {Count} keys from index", keys.Count);

                var items = await _dapr.GetBulkStateAsync(ConstantValues.V2Content.ContentStoreName, keys, null, cancellationToken: cancellationToken);

                var News = items
                    .Select(i => JsonSerializer.Deserialize<V2NewsCategory>(i.Value, new JsonSerializerOptions
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
    }
}