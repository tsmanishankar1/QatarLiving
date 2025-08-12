using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        private static int _nextCategoryId = 101;
        private static int _nextSubCategoryId = 1001;

        public V2InternalNewsService(DaprClient dapr, ILogger<IV2NewsService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }
        public async Task<string> CreateWritertagAsync(Writertag dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Tagname))
                {
                    throw new ArgumentException("Tagname is required.");
                }

                var key = $"writertag-{dto.tagId}";
                string storeName = V2Content.ContentStoreName;
                await _dapr.SaveStateAsync(storeName, key, dto, cancellationToken: cancellationToken);

                string indexKey = "writertags-index";
                var currentTags = await _dapr.GetStateAsync<List<string>>(storeName, indexKey) ?? new();
                if (!currentTags.Contains(key))
                {
                    currentTags.Add(key);
                    await _dapr.SaveStateAsync(storeName, indexKey, currentTags, cancellationToken: cancellationToken);
                }

                return "Writer tag created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create writer tag");
                throw new Exception("Unexpected error during writer tag creation", ex);
            }
        }
        public async Task<List<Writertag>> GetAllWritertagsAsync(CancellationToken cancellationToken)
        {
            try
            {
                string storeName = V2Content.ContentStoreName;
                string indexKey = "writertags-index";

                var tagKeys = await _dapr.GetStateAsync<List<string>>(storeName, indexKey) ?? new();

                var tags = new List<Writertag>();

                foreach (var key in tagKeys)
                {
                    var tag = await _dapr.GetStateAsync<Writertag>(storeName, key);
                    if (tag != null)
                    {
                        tags.Add(tag);
                    }
                }

                return tags;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve writer tags");
                throw;
            }
        }
        public async Task<string> DeleteTagName(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var storeName = V2Content.ContentStoreName;
                var key = $"writertag-{id}";

                await _dapr.DeleteStateAsync(storeName, key);

                var indexKey = "writertags-index";
                var keys = await _dapr.GetStateAsync<List<string>>(storeName, indexKey) ?? new();
                if (keys.Contains(key))
                {
                    keys.Remove(key);
                    await _dapr.SaveStateAsync(storeName, indexKey, keys);
                }

                return "Writer tag deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting writer tag with id {id}");
                throw new Exception("Unexpected error during deletion", ex);
            }
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
                string storeName = V2Content.ContentStoreName;
                var duplicateCheck = dto.Categories
                    .GroupBy(c => new { c.CategoryId, c.SubcategoryId })
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateCheck.Any())
                {
                    var duplicates = string.Join(", ", duplicateCheck.Select(h => $"CategoryId:{h.CategoryId}, SubCategoryId:{h.SubcategoryId}"));
                    throw new InvalidDataException($"Duplicate category and subcategory combinations are not allowed in the same request. Duplicates: {duplicates}");
                }

                // This will be very slow

                //var existingArticles = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.NewsIndexKey, cancellationToken : cancellationToken)
                //                        ?? new List<string>();

                //foreach (var existingId in existingArticles)
                //{
                //    var existingArticle = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, existingId, cancellationToken : cancellationToken);
                //    if (existingArticle != null &&
                //        string.Equals(existingArticle.Title.Trim(), dto.Title.Trim(), StringComparison.OrdinalIgnoreCase))
                //    {
                //        throw new InvalidDataException($"A news article with the title '{dto.Title}' already exists.");
                //    }
                //}

                var slugBase = GenerateNewsSlug(dto.Title);
                var articleId = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id; // check if the DTO already has a GUID assigned
                                                                                // and only generate a new one if this is GUID.Empty.

                var articleCategories = dto.Categories.Select(cat => new V2ArticleCategory
                {
                    CategoryId = cat.CategoryId,
                    SubcategoryId = cat.SubcategoryId,
                    SlotId = cat.SlotId == 0 ? (int)Slot.UnPublished : cat.SlotId
                }).ToList();

                var article = new V2NewsArticleDTO
                {
                    Id = articleId,
                    Title = dto.Title,
                    Content = dto.Content,
                    WriterTag = dto.WriterTag,
                    Slug = slugBase,
                    IsActive = true,
                    Categories = articleCategories,
                    PublishedDate = articleCategories.Any(c => c.SlotId != (int)Slot.UnPublished) ? DateTime.UtcNow : (DateTime?)null,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    authorName = dto.authorName,
                    CoverImageUrl = dto.CoverImageUrl,
                    UserId = dto.UserId
                };

                string articleIdStr = article.Id.ToString();

                await _dapr.SaveStateAsync(storeName, articleIdStr, article, cancellationToken: cancellationToken);

                foreach (var cat in article.Categories)
                {
                    if (cat.SlotId > 0)
                    {
                        await HandleSlotShiftAsync(cat.CategoryId, cat.SubcategoryId, cat.SlotId, article, cancellationToken);
                    }
                    else
                    {
                        string statusKey = GetStatusSlotKey(cat.CategoryId, cat.SubcategoryId, cat.SlotId);
                        await _dapr.SaveStateAsync(storeName, statusKey, articleIdStr, cancellationToken: cancellationToken);
                    }
                }

                var indexKey = V2Content.NewsIndexKey;
                var currentIndex = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, indexKey, cancellationToken: cancellationToken)
                                    ?? new List<string>();

                if (!currentIndex.Contains(articleIdStr))
                {
                    currentIndex.Add(articleIdStr);
                    await _dapr.SaveStateAsync(V2Content.ContentStoreName, indexKey, currentIndex, cancellationToken: cancellationToken);
                }

                var upsertRequest = await IndexNewsToAzureSearch(article, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
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

        public async Task<string> BulkMigrateNewsArticleAsync(List<V2NewsArticleDTO> articles, CancellationToken cancellationToken = default)
        {
            try
            {
                string storeName = V2Content.ContentStoreName;
                string indexKey = V2Content.NewsIndexKey;

                foreach (var article in articles)
                {
                    var articleIdStr = article.Id.ToString();

                    await _dapr.SaveStateAsync(storeName, articleIdStr, article, cancellationToken: cancellationToken);

                    var currentIndex = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, indexKey, cancellationToken: cancellationToken)
                                        ?? new List<string>();

                    if (!currentIndex.Contains(articleIdStr))
                    {
                        currentIndex.Add(articleIdStr);
                        await _dapr.SaveStateAsync(V2Content.ContentStoreName, indexKey, currentIndex, cancellationToken: cancellationToken);
                    }

                    var upsertRequest = await IndexNewsToAzureSearch(article, cancellationToken);
                    if (upsertRequest != null)
                    {
                        var message = new IndexMessage
                        {
                            Action = "Upsert",
                            Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                            UpsertRequest = upsertRequest
                        };

                        await _dapr.PublishEventAsync(
                            pubsubName: ConstantValues.PubSubName,
                            topicName: ConstantValues.PubSubTopics.IndexUpdates,
                            data: message,
                            cancellationToken: cancellationToken
                        );
                    }
                }

                return "News articles created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create article");
                throw new Exception("Unexpected error during article creation", ex);
            }
        }

        public async Task<string> MigrateNewsArticleAsync(V2NewsArticleDTO article, CancellationToken cancellationToken = default)
        {
            try
            {
                string storeName = V2Content.ContentStoreName;
                string indexKey = V2Content.NewsIndexKey;

                var articleIdStr = article.Id.ToString();

                await _dapr.SaveStateAsync(storeName, articleIdStr, article, cancellationToken: cancellationToken);

                var currentIndex = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, indexKey, cancellationToken: cancellationToken)
                                    ?? new List<string>();

                if (!currentIndex.Contains(articleIdStr))
                {
                    currentIndex.Add(articleIdStr);
                    await _dapr.SaveStateAsync(V2Content.ContentStoreName, indexKey, currentIndex, cancellationToken: cancellationToken);
                }

                var upsertRequest = await IndexNewsToAzureSearch(article, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return "News articles created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create article");
                throw new Exception("Unexpected error during article creation", ex);
            }
        }

        private async Task<string> HandleSlotShiftAsync(int categoryId, int subCategoryId, int desiredSlot, V2NewsArticleDTO newArticle, CancellationToken cancellationToken)
        {
            const int MaxSlotToShift = 50;
            string storeName = V2Content.ContentStoreName;

            try
            {
                _logger.LogInformation($"Start shifting articles for category: {categoryId}, subcategory: {subCategoryId}, starting at slot {desiredSlot}");

                for (int currentSlot = MaxSlotToShift; currentSlot >= desiredSlot; currentSlot--)
                {
                    var currentSlotKey = GetSlotKey(categoryId, subCategoryId, currentSlot);
                    var nextSlotKey = GetSlotKey(categoryId, subCategoryId, currentSlot + 1);

                    _logger.LogDebug($"Checking slot {currentSlot} (key: {currentSlotKey})");

                    string existingArticleId = null;
                    try
                    {
                        existingArticleId = await _dapr.GetStateAsync<string>(V2Content.ContentStoreName, currentSlotKey, cancellationToken: cancellationToken);
                    }
                    catch (Exception daprEx)
                    {
                        _logger.LogWarning(daprEx, $"Dapr error when fetching state for slot {currentSlotKey}");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(existingArticleId))
                    {
                        V2NewsArticleDTO articleToMove = null;
                        try
                        {
                            articleToMove = await _dapr.GetStateAsync<V2NewsArticleDTO>(storeName, existingArticleId, cancellationToken: cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to get article {existingArticleId} from state store.");
                            continue;
                        }

                        if (articleToMove?.Categories == null)
                        {
                            _logger.LogWarning($"Article {existingArticleId} has no categories. Skipping.");
                            continue;
                        }

                        var articleCat = articleToMove.Categories.FirstOrDefault(c =>
                            c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);

                        if (articleCat == null)
                        {
                            _logger.LogWarning($"No matching category/subcategory found in article {existingArticleId}. Skipping.");
                            continue;
                        }

                        articleCat.SlotId = currentSlot + 1;

                        await _dapr.SaveStateAsync(storeName, articleToMove.Id.ToString(), articleToMove, cancellationToken: cancellationToken);
                        await _dapr.SaveStateAsync(V2Content.ContentStoreName, nextSlotKey, articleToMove.Id.ToString(), cancellationToken: cancellationToken);
                        await _dapr.DeleteStateAsync(V2Content.ContentStoreName, currentSlotKey, cancellationToken: cancellationToken);
                        var upsertMoved = await IndexNewsToAzureSearch(articleToMove, cancellationToken);
                        if (upsertMoved != null)
                        {
                            var msgMoved = new IndexMessage
                            {
                                Action = "Upsert",
                                Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                                UpsertRequest = upsertMoved
                            };
                            await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msgMoved, cancellationToken);
                        }

                        _logger.LogInformation($"Shifted article {existingArticleId} from slot {currentSlot} to {currentSlot + 1}");
                    }
                }

                // Place new article in the desired slot
                if (newArticle.Categories == null)
                {
                    _logger.LogError($"New article {newArticle.Id} has no categories defined.");
                    throw new InvalidOperationException("New article is missing categories.");
                }

                var newArticleCat = newArticle.Categories.FirstOrDefault(c =>
                    c.CategoryId == categoryId && c.SubcategoryId == subCategoryId);

                if (newArticleCat == null)
                {
                    _logger.LogError($"New article {newArticle.Id} has no matching category/subcategory for slot assignment.");
                    throw new InvalidOperationException("Cannot place article in slot without matching category.");
                }

                newArticleCat.SlotId = desiredSlot;

                await _dapr.SaveStateAsync(storeName, newArticle.Id.ToString(), newArticle, cancellationToken: cancellationToken);

                var desiredSlotKey = GetSlotKey(categoryId, subCategoryId, desiredSlot);
                await _dapr.SaveStateAsync(V2Content.ContentStoreName, desiredSlotKey, newArticle.Id.ToString(), cancellationToken: cancellationToken);

                var upsertNew = await IndexNewsToAzureSearch(newArticle, cancellationToken);
                if (upsertNew != null)
                {
                    var msgNew = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                        UpsertRequest = upsertNew
                    };
                    await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msgNew, cancellationToken);
                }

                _logger.LogInformation($"New article {newArticle.Id} placed into slot {desiredSlot}");

                return $"Article placed in slot {desiredSlot} and older articles shifted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Slot shift failed for category {categoryId}/{subCategoryId}, desiredSlot {desiredSlot}");
                throw; // Rethrow to let caller see original exception
            }
        }

        private string GetSlotKey(int categoryId, int subCategoryId, int slot) =>
           $"slot-article-{categoryId}-{subCategoryId}-slot{slot}";

        private string GetStatusSlotKey(int categoryId, int subCategoryId, int slot) =>
            $"slot-article-status-{categoryId}-{subCategoryId}-slot{slot}";

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

            var upsertRequest = await IndexNewsToAzureSearch(existing, cancellationToken);
            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }

            return "News Soft Deleted Successfully";
        }

        public async Task<string> UpdateNewsArticleAsync(
        V2NewsArticleDTO dto,
        CancellationToken cancellationToken = default)
        {
            const int MaxLiveSlot = 13;
            var store = V2Content.ContentStoreName;
            var articleId = dto.Id;

            try
            {
                string storeName = V2Content.ContentStoreName;
                if (dto.Id == Guid.Empty)
                    throw new InvalidDataException("Id is required for update");

                var key = dto.Id.ToString();
                var existing = await _dapr.GetStateAsync<V2NewsArticleDTO>(
                    store, key, cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException("News article not found");

               /* var existingArticles = await _dapr.GetStateAsync<List<string>>(storeName, V2Content.NewsIndexKey, cancellationToken: cancellationToken)
                                        ?? new List<string>();

                foreach (var existingId in existingArticles)
                {
                    if (existingId == dto.Id.ToString())
                        continue;

                    var existingArticle = await _dapr.GetStateAsync<V2NewsArticleDTO>(
                        storeName, existingId, cancellationToken: cancellationToken);

                    if (existingArticle != null &&
                        string.Equals(existingArticle.Title.Trim(), dto.Title.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"A news article with the title '{dto.Title}' already exists.");
                    }
                }*/

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
                        var oldSlotKey = GetSlotKey(oldCat.CategoryId, oldCat.SubcategoryId, oldSlot);
                        await _dapr.DeleteStateAsync(store, oldSlotKey, cancellationToken: cancellationToken);
                    }

                    await _dapr.SaveStateAsync(store, key, dto, cancellationToken: cancellationToken);
                }

                dto.UpdatedAt = DateTime.UtcNow;
                await _dapr.SaveStateAsync(store, key, dto, cancellationToken: cancellationToken);

                var upsertUpdated = await IndexNewsToAzureSearch(dto, cancellationToken);
                if (upsertUpdated != null)
                {
                    var msgUpdated = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                        UpsertRequest = upsertUpdated
                    };
                    await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msgUpdated, cancellationToken);
                }

                _logger.LogInformation("News article {ArticleId} updated successfully.", dto.Id);

                var upsertRequest = await IndexNewsToAzureSearch(dto, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return "News article updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating news article {ArticleId}", articleId);
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

                await _dapr.SaveStateAsync(storeName, slotKey, article, cancellationToken: cancellationToken);
                await _dapr.SaveStateAsync(storeName, article.Id.ToString(), article, cancellationToken: cancellationToken);

                var upsertReordered = await IndexNewsToAzureSearch(article, cancellationToken);
                if (upsertReordered != null)
                {
                    var msgReordered = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentNewsIndex,
                        UpsertRequest = upsertReordered
                    };
                    await _dapr.PublishEventAsync(ConstantValues.PubSubName, ConstantValues.PubSubTopics.IndexUpdates, msgReordered, cancellationToken);
                }
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

        private async Task<CommonIndexRequest> IndexNewsToAzureSearch(QLN.Common.DTO_s.V2NewsArticleDTO dto, CancellationToken cancellationToken)
        {

            var indexDoc = new ContentNewsIndex
            {
                Id = dto.Id.ToString(),
                Title = dto.Title,
                Content = dto.Content,
                authorName = dto.authorName,
                CoverImageUrl = dto.CoverImageUrl,
                Slug = dto.Slug,
                UserId = dto.UserId,
                WriterTag = dto.WriterTag,
                PublishedDate = dto.PublishedDate,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Categories = dto.Categories.Select(i => new ArticleCategory
                {
                    CategoryId = i.CategoryId,
                    SubcategoryId = i.SubcategoryId,
                    SlotId = i.SlotId
                }).ToList()
            };

            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ContentNewsIndex,
                ContentNewsItem = indexDoc
            };
            return indexRequest;

        }

        public Task<PagedResponse<V2NewsArticleDTO>> GetAllNewsArticlesAsync(int? page, int? perPage, string? search, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(int categoryId, int subCategoryId, ArticleStatus status, string? search, int? page, int? pageSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsArticleDTO>> GetAllNewsFilterArticles(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2NewsArticleDTO?> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Common.Infrastructure.DTO_s.GenericNewsPageResponse> GetNewsLandingPageAsync(int categoryId, int subCategoryId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}



