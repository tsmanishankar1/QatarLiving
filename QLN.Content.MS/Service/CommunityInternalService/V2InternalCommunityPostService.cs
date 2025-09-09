using Dapr.Client;
using Microsoft.Extensions.Hosting;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using static QLN.Common.DTO_s.CommunityBo;
using System.Linq;

namespace QLN.Content.MS.Service.CommunityInternalService
{
    public class V2InternalCommunityPostService : IV2CommunityPostService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2InternalCommunityPostService> _logger;
        private static string GetKey(Guid id) => $"community-{id}";
        private const string StoreName = "contentstatestore";
        private const string IndexKey = "community-index";

        private static readonly JsonSerializerOptions _jsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public V2InternalCommunityPostService(DaprClient dapr, ILogger<V2InternalCommunityPostService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateCommunityPostAsync(string userId, V2CommunityPostDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Title is required.");
            if (string.IsNullOrWhiteSpace(dto.Description)) throw new ArgumentException("Description is required.");

            dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
            dto.UpdatedBy = userId;
            dto.UpdatedDate = DateTime.UtcNow;
            dto.DateCreated = DateTime.UtcNow;
            dto.Slug = ProcessingHelpers.GenerateSlug(dto.Title);
            NormalizeCommunityDto(dto);
            var key = GetKey(dto.Id);

            try
            {
                var existing = await _dapr.GetStateAsync<object>(StoreName, key, cancellationToken: ct);
                if (existing != null)
                    throw new InvalidOperationException($"Community post with key {key} already exists.");

                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                    await _dapr.SaveStateAsync(StoreName, IndexKey, index, cancellationToken: ct);
                }

                var upsertRequest = await IndexCommunityPostToAzureSearch(dto, cancellationToken: ct);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentCommunityIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: ct
                    );
                }

                return "Community post created successfully";
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning(ex, "Duplicate community post insert attempt.");
                throw new InvalidOperationException("Community post already exists. Conflict occurred during creation.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed in CreateCommunityPostAsync");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error while creating community post.");
                throw new ConflictException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during community post creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the community post. Please try again later.", ex);
            }
        }

        public async Task<string> BulkMigrateCommunityPostsAsync(List<V2CommunityPostDto> posts, CancellationToken ct = default)
        {
            foreach (var dto in posts)
            {
                try
                {
                    NormalizeCommunityDto(dto);
                    var key = GetKey(dto.Id);

                    await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);

                    var index = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct) ?? new();

                    if (!index.Contains(key))
                    {
                        index.Add(key);
                        await _dapr.SaveStateAsync(StoreName, IndexKey, index, cancellationToken: ct);
                    }

                    var upsertRequest = await IndexCommunityPostToAzureSearch(dto, cancellationToken: ct);
                    if (upsertRequest != null)
                    {
                        var message = new IndexMessage
                        {
                            Action = "Upsert",
                            Vertical = ConstantValues.IndexNames.ContentCommunityIndex,
                            UpsertRequest = upsertRequest
                        };

                        await _dapr.PublishEventAsync(
                            pubsubName: ConstantValues.PubSubName,
                            topicName: ConstantValues.PubSubTopics.IndexUpdates,
                            data: message,
                            cancellationToken: ct
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Unhandled error occurred during community post bulk migration.");
                    throw new InvalidOperationException("An unexpected error occurred while creating the community post. Please try again later.", ex);
                }
            }

            return "Community posts created successfully";
        }

        public async Task<string> MigrateCommunityPostAsync(V2CommunityPostDto dto, CancellationToken ct = default)
        {
            try
            {
                var key = GetKey(dto.Id);

                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct) ?? new();

                if (!index.Contains(key))
                {
                    index.Add(key);
                    await _dapr.SaveStateAsync(StoreName, IndexKey, index, cancellationToken: ct);
                }

                var upsertRequest = await IndexCommunityPostToAzureSearch(dto, cancellationToken: ct);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentCommunityIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: ct
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during community post migration.");
                throw new InvalidOperationException("An unexpected error occurred while creating the community post. Please try again later.", ex);
            }
            return "Community posts created successfully";
        }

        public Task<ForumCategoryListDto> GetAllForumCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categories = new List<ForumCategoryDto>
            {
                new ForumCategoryDto { Id = "20000005", Name = "Family Life in Qatar" },
                new ForumCategoryDto { Id = "20000006", Name = "Welcome to Qatar" },
                new ForumCategoryDto { Id = "20000008", Name = "Socialising" },
                new ForumCategoryDto { Id = "20000011", Name = "Qatari Culture" },
                new ForumCategoryDto { Id = "20000012", Name = "Working in Qatar" },
                new ForumCategoryDto { Id = "20000013", Name = "Opportunities" },
                new ForumCategoryDto { Id = "20000014", Name = "Salary & Allowances" },
                new ForumCategoryDto { Id = "20000016", Name = "Qatar Living Website" },
                new ForumCategoryDto { Id = "20000017", Name = "Missing home!" },
                new ForumCategoryDto { Id = "20000018", Name = "Politics" },
                new ForumCategoryDto { Id = "20000019", Name = "Advice and Help" },
                new ForumCategoryDto { Id = "20000020", Name = "Qatar Living Lounge " },
                new ForumCategoryDto { Id = "20000021", Name = "Funnies" },
                new ForumCategoryDto { Id = "20000022", Name = "Language" },
                new ForumCategoryDto { Id = "20000023", Name = "Beauty and Style" },
                new ForumCategoryDto { Id = "20000026", Name = "Computers and Internet" },
                new ForumCategoryDto { Id = "20000027", Name = "Electronics & Gadgets" },
                new ForumCategoryDto { Id = "20000029", Name = "Health and Fitness" },
                new ForumCategoryDto { Id = "20000030", Name = "Pets and Animals" },
                new ForumCategoryDto { Id = "20000033", Name = "Qatar 2022" },
                new ForumCategoryDto { Id = "20000034", Name = "Company News" },
                new ForumCategoryDto { Id = "20000035", Name = "Ramadan & Eid" },
                new ForumCategoryDto { Id = "20000036", Name = "Recipes" },
                new ForumCategoryDto { Id = "20000037", Name = "Dining" },
                new ForumCategoryDto { Id = "20000038", Name = "Fashion" },
                new ForumCategoryDto { Id = "20000039", Name = "Technology & Internet" },
                new ForumCategoryDto { Id = "29113511", Name = "Movies in Qatar" },
                new ForumCategoryDto { Id = "31632626", Name = "Kid's Corner" },
                new ForumCategoryDto { Id = "20000025", Name = "Motoring" },
                new ForumCategoryDto { Id = "20000015", Name = "Visas and Permits" },
                new ForumCategoryDto { Id = "20000010", Name = "Travel and Tourism" },
                new ForumCategoryDto { Id = "20000009", Name = "Doha Shopping" },
                new ForumCategoryDto { Id = "20000032", Name = "Sports in Qatar" },
                new ForumCategoryDto { Id = "33607306", Name = "Money Matter & Cost of Living" },
                new ForumCategoryDto { Id = "20000028", Name = "Education" },
                new ForumCategoryDto { Id = "20000024", Name = "Business & Finance" },
                new ForumCategoryDto { Id = "27449576", Name = "Arts & Culture" },
                new ForumCategoryDto { Id = "20000007", Name = "Moving to Qatar" },
                new ForumCategoryDto { Id = "41696191", Name = "World Cup" }
            };
            return Task.FromResult(new ForumCategoryListDto { ForumCategories = categories });
        }

        public async Task<bool> SoftDeleteCommunityPostAsync(Guid postId, string userId, CancellationToken ct = default)
        {
            var key = GetKey(postId);
            var post = await _dapr.GetStateAsync<V2CommunityPostDto>(StoreName, key, cancellationToken: ct);

            if (post == null || post.Id == Guid.Empty)
                throw new KeyNotFoundException($"No post found with id {postId}.");

            post.IsActive = false;
            post.UpdatedBy = userId;
            post.UpdatedDate = DateTime.UtcNow;

            await _dapr.SaveStateAsync(StoreName, key, post, cancellationToken: ct);

            var upsertRequest = await IndexCommunityPostToAzureSearch(post, cancellationToken: ct);
            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ContentCommunityIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: ct
                );
            }

            return true;
        }

        public async Task<bool> LikePostForUser(CommunityPostLikeDto dto, CancellationToken ct = default)
        {
            var key = $"like-{dto.CommunityPostId}-{dto.UserId}";
            var indexKey = $"like-index-{dto.CommunityPostId}";

            try
            {
                var existing = await _dapr.GetStateAsync<CommunityPostLikeDto>(StoreName, key, cancellationToken: ct);
                var userIndex = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (existing != null)
                {
                    // Already liked → remove like (dislike)
                    await _dapr.DeleteStateAsync(StoreName, key, cancellationToken: ct);

                    userIndex.Remove(dto.UserId);
                    await _dapr.SaveStateAsync(StoreName, indexKey, userIndex, cancellationToken: ct);

                    _logger.LogInformation("User {UserId} unliked post {PostId}", dto.UserId, dto.CommunityPostId);

                    // Update index counts
                    await PublishCommunityIndexUpdateAsync(dto.CommunityPostId, ct);
                    return false; // unliked
                }

                // Not liked → add like
                dto.LikePostId = Guid.NewGuid();
                dto.LikedDate = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);

                if (!userIndex.Contains(dto.UserId))
                    userIndex.Add(dto.UserId);

                await _dapr.SaveStateAsync(StoreName, indexKey, userIndex, cancellationToken: ct);

                _logger.LogInformation("User {UserId} liked post {PostId}", dto.UserId, dto.CommunityPostId);

                // Update index counts
                await PublishCommunityIndexUpdateAsync(dto.CommunityPostId, ct);
                return true; // liked
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post {PostId}", dto.CommunityPostId);
                throw;
            }
        }

        public async Task AddCommentToCommunityPostAsync(CommunityCommentDto dto, CancellationToken ct = default)
        {
            var key = $"comment-{dto.CommunityPostId}-{dto.CommentId}";
            var indexKey = $"comment-index-{dto.CommunityPostId}";

            try
            {
                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);

                var index = await _dapr.GetStateAsync<List<Guid>>(StoreName, indexKey, cancellationToken: ct) ?? new();
                if (!index.Contains(dto.CommentId))
                {
                    index.Add(dto.CommentId);
                    await _dapr.SaveStateAsync(StoreName, indexKey, index, cancellationToken: ct);
                }

                _logger.LogInformation("Comment added to community post {PostId}", dto.CommunityPostId);

                // Update index counts
                await PublishCommunityIndexUpdateAsync(dto.CommunityPostId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to community post {PostId}", dto.CommunityPostId);
                throw;
            }
        }

        public async Task<CommunityCommentListResponse> GetAllCommentsByPostIdAsync(Guid postId, string? userId, int? page = null, int? perPage = null, CancellationToken ct = default)
        {
            var indexKey = $"comment-index-{postId}";

            try
            {
                var commentIds = await _dapr.GetStateAsync<List<Guid>>(StoreName, indexKey, cancellationToken: ct) ?? new();

                var commentKeys = commentIds
                    .Select(id => $"comment-{postId}-{id}")
                    .ToList();

                var commentStates = await _dapr.GetBulkStateAsync(
                    storeName: StoreName,
                    keys: commentKeys,
                    parallelism: null,
                    metadata: null,
                    cancellationToken: ct
                );

                var allComments = new List<CommunityCommentDto>();

                foreach (var state in commentStates)
                {
                    if (string.IsNullOrWhiteSpace(state.Value)) continue;

                    try
                    {
                        var comment = JsonSerializer.Deserialize<CommunityCommentDto>(state.Value, _jsonOpts);

                        if (comment != null && comment.IsActive && comment.CommentId != Guid.Empty)
                        {
                            var likeIndexKey = $"comment-like-index-{comment.CommentId}";
                            var likedUsers = await _dapr.GetStateAsync<List<string>>(StoreName, likeIndexKey, cancellationToken: ct) ?? new();
                            comment.CommentsLikeCount = likedUsers.Count;
                            comment.LikedUserIds = likedUsers;

                            allComments.Add(comment);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize comment for key: {Key}", state.Key);
                    }
                }

                int currentPage = page ?? 1;
                int itemsPerPage = perPage ?? 10;
                int skip = (currentPage - 1) * itemsPerPage;

                var grouped = allComments
                    .GroupBy(c => c.ParentCommentId ?? Guid.Empty)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = new List<CommunityCommentItem>();

                List<CommunityCommentItem> BuildReplies(Guid parentId)
                {
                    if (!grouped.ContainsKey(parentId))
                        return new();

                    return grouped[parentId]
                        .Select(reply => new CommunityCommentItem
                        {
                            CommentId = reply.CommentId,
                            UserId = reply.UserId,
                            UserName = reply.UserName,
                            Content = reply.Content,
                            CommentedAt = reply.CommentedAt,
                            LikeCount = reply.CommentsLikeCount,
                            LikedUserIds = reply.LikedUserIds ?? new(),
                            CommentedUserId = reply.UserId,
                            IsLiked = !string.IsNullOrEmpty(userId) && (reply.LikedUserIds?.Contains(userId) ?? false),
                            Replies = BuildReplies(reply.CommentId)
                        })
                        .ToList();
                }

                if (grouped.ContainsKey(Guid.Empty))
                {
                    var rootParents = grouped[Guid.Empty]
                        .Skip(skip)
                        .Take(itemsPerPage)
                        .ToList();

                    foreach (var parent in rootParents)
                    {
                        var parentItem = new CommunityCommentItem
                        {
                            CommentId = parent.CommentId,
                            UserId = parent.UserId,
                            UserName = parent.UserName,
                            Content = parent.Content,
                            CommentedAt = parent.CommentedAt,
                            LikeCount = parent.CommentsLikeCount,
                            LikedUserIds = parent.LikedUserIds ?? new(),
                            CommentedUserId = parent.UserId,
                            IsLiked = !string.IsNullOrEmpty(userId) && (parent.LikedUserIds?.Contains(userId) ?? false),
                            Replies = BuildReplies(parent.CommentId)
                        };

                        result.Add(parentItem);
                    }
                }

                return new CommunityCommentListResponse
                {
                    TotalComments = grouped.ContainsKey(Guid.Empty) ? grouped[Guid.Empty].Count : 0,
                    PerPage = itemsPerPage,
                    CurrentPage = currentPage,
                    Comments = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comment list for post {PostId}", postId);
                throw;
            }
        }

        public async Task<bool> LikeCommentAsync(LikeCommentsDto likeCommentsDto, string userId, CancellationToken ct = default)
        {
            var key = $"comment-like-{likeCommentsDto.CommentId}-{userId}";
            var indexKey = $"comment-like-index-{likeCommentsDto.CommentId}";

            try
            {
                var existing = await _dapr.GetStateAsync<string>(StoreName, key);
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    await _dapr.DeleteStateAsync(StoreName, key);
                    index.Remove(userId);
                    await _dapr.SaveStateAsync(StoreName, indexKey, index);

                    _logger.LogInformation("User {UserId} unliked comment {CommentId}", userId, likeCommentsDto.CommentId);
                    return false;
                }

                await _dapr.SaveStateAsync(StoreName, key, userId);

                if (!index.Contains(userId))
                    index.Add(userId);

                await _dapr.SaveStateAsync(StoreName, indexKey, index);

                _logger.LogInformation("User {UserId} liked comment {CommentId}", userId, likeCommentsDto.CommentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing comment like for {CommentId}", likeCommentsDto.CommentId);
                throw;
            }
        }

        public async Task<CommunityCommentApiResponse> SoftDeleteCommunityCommentAsync(Guid postId, Guid commentId, string userId, CancellationToken ct = default)
        {
            var commentKey = $"comment-{postId}-{commentId}";
            _logger.LogInformation("Attempting to soft delete community comment {CommentId} for post {PostId}", commentId, postId);

            try
            {
                var comment = await _dapr.GetStateAsync<CommunityCommentDto>(StoreName, commentKey, cancellationToken: ct);
                if (comment == null)
                {
                    _logger.LogWarning("Comment not found for key: {CommentKey}", commentKey);
                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Comment not found"
                    };
                }

                if (!comment.IsActive)
                {
                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Comment already deleted"
                    };
                }

                if (!string.Equals(comment.UserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = "You are not authorized to delete this comment"
                    };
                }

                comment.IsActive = false;
                await _dapr.SaveStateAsync(StoreName, commentKey, comment, cancellationToken: ct);
                _logger.LogInformation("Soft-deleted comment ID: {CommentId} from post {PostId}", commentId, postId);

                // Soft delete replies
                var indexKey = $"comment-index-{postId}";
                var commentIds = await _dapr.GetStateAsync<List<Guid>>(StoreName, indexKey, cancellationToken: ct) ?? new();

                foreach (var childId in commentIds)
                {
                    var childKey = $"comment-{postId}-{childId}";
                    var childComment = await _dapr.GetStateAsync<CommunityCommentDto>(StoreName, childKey, cancellationToken: ct);
                    if (childComment != null && childComment.IsActive && childComment.ParentCommentId == commentId)
                    {
                        childComment.IsActive = false;
                        await _dapr.SaveStateAsync(StoreName, childKey, childComment, cancellationToken: ct);
                        _logger.LogInformation("Also soft-deleted reply comment ID: {ChildCommentId}", childId);
                    }
                }

                // Update index counts
                await PublishCommunityIndexUpdateAsync(postId, ct);

                return new CommunityCommentApiResponse
                {
                    Status = "success",
                    Message = "Comment and its replies deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting community comment {CommentId} on post {PostId}", commentId, postId);
                return new CommunityCommentApiResponse
                {
                    Status = "failed",
                    Message = "Error occurred while deleting comment"
                };
            }
        }

        public async Task<CommunityCommentApiResponse> EditCommunityCommentAsync(Guid postId, Guid commentId, string userId, string updatedText, CancellationToken ct = default)
        {
            var commentKey = $"comment-{postId}-{commentId}";

            try
            {
                _logger.LogInformation("Attempting to edit comment {CommentId} on post {PostId} by user {UserId}", commentId, postId, userId);

                var comment = await _dapr.GetStateAsync<CommunityCommentDto>(StoreName, commentKey, cancellationToken: ct);

                if (comment == null)
                {
                    _logger.LogWarning("Comment not found for key {CommentKey}", commentKey);
                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Comment not found"
                    };
                }

                if (!comment.IsActive)
                {
                    _logger.LogWarning("Attempt to edit inactive comment {CommentId}", commentId);
                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Cannot edit a deleted comment"
                    };
                }

                if (!string.Equals(comment.UserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Unauthorized edit attempt. Comment owner: {CommentUserId}, Requesting user: {RequestUserId}", comment.UserId, userId);
                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = "You are not authorized to edit this comment"
                    };
                }

                if (string.IsNullOrWhiteSpace(updatedText))
                {
                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = "Updated text cannot be empty"
                    };
                }

                comment.Content = updatedText;
                comment.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, commentKey, comment, cancellationToken: ct);

                _logger.LogInformation("Successfully updated comment {CommentId}", commentId);

                // No count changes, so no index update required; call it if you want UpdatedDate freshness:
                // await PublishCommunityIndexUpdateAsync(postId, ct);

                return new CommunityCommentApiResponse
                {
                    Status = "success",
                    Message = "Comment updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing community comment {CommentId} for post {PostId} by user {UserId}", commentId, postId, userId);
                return new CommunityCommentApiResponse
                {
                    Status = "failed",
                    Message = "Error occurred while editing comment"
                };
            }
        }

        private async Task<CommonIndexRequest> IndexCommunityPostToAzureSearch(V2CommunityPostDto dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ContentCommunityIndex
            {
                Id = dto.Id.ToString(),
                UserName = dto.UserName,
                Title = dto.Title,
                Slug = dto.Slug,
                Description = dto.Description,
                Category = dto.Category,
                CategoryId = dto.CategoryId,
                CommentedUserIds = (dto.CommentedUserIds ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s != "string").Distinct().ToList(),
                CommentCount = dto.CommentCount,
                LikedUserIds = (dto.LikedUserIds ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s != "string").Distinct().ToList(),
                LikeCount = dto.LikeCount,
                ImageUrl = dto.ImageUrl,
                UserId = dto.UserId,
                IsActive = dto.IsActive,
                DateCreated = dto.DateCreated,
                UpdatedBy = dto.UpdatedBy,
                UpdatedDate = dto.UpdatedDate
            };

            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ContentCommunityIndex,
                ContentCommunityItem = indexDoc
            };
            return indexRequest;
        }

        private static void NormalizeCommunityDto(V2CommunityPostDto dto)
        {
            dto.Title = dto.Title?.Trim() ?? string.Empty;
            dto.Description ??= string.Empty;

            dto.LikedUserIds = (dto.LikedUserIds ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != "string")
                .Distinct()
                .ToList();

            dto.CommentedUserIds = (dto.CommentedUserIds ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != "string")
                .Distinct()
                .ToList();

            dto.LikeCount = dto.LikedUserIds.Count;
            dto.CommentCount = dto.CommentCount < 0 ? 0 : dto.CommentCount;

            if (dto.DateCreated == default) dto.DateCreated = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(dto.Slug) && !string.IsNullOrWhiteSpace(dto.Title))
                dto.Slug = ProcessingHelpers.GenerateSlug(dto.Title);
        }

        /// <summary>
        /// Rebuilds post like/comment aggregates from state and publishes an index upsert so external reads (via search) stay fresh.
        /// </summary>
        private async Task PublishCommunityIndexUpdateAsync(Guid postId, CancellationToken ct)
        {
            var postKey = GetKey(postId);
            var post = await _dapr.GetStateAsync<V2CommunityPostDto>(StoreName, postKey, cancellationToken: ct);
            if (post is null) return;

            // Rebuild likes
            var likeIndexKey = $"like-index-{postId}";
            var likedUsers = await _dapr.GetStateAsync<List<string>>(StoreName, likeIndexKey, cancellationToken: ct) ?? new();
            post.LikedUserIds = likedUsers
                .Where(u => !string.IsNullOrWhiteSpace(u) && u != "string")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            post.LikeCount = post.LikedUserIds.Count;

            // Rebuild comments
            var commentIndexKey = $"comment-index-{postId}";
            var commentIds = await _dapr.GetStateAsync<List<Guid>>(StoreName, commentIndexKey, cancellationToken: ct) ?? new();

            var commenters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var activeCommentCount = 0;

            if (commentIds.Count > 0)
            {
                var commentKeys = commentIds.Select(id => $"comment-{postId}-{id}").ToList();
                var states = await _dapr.GetBulkStateAsync(StoreName, commentKeys, parallelism: null, cancellationToken: ct);
                foreach (var s in states)
                {
                    if (string.IsNullOrWhiteSpace(s.Value)) continue;
                    var c = JsonSerializer.Deserialize<CommunityCommentDto>(s.Value, _jsonOpts);
                    if (c is { IsActive: true })
                    {
                        activeCommentCount++;
                        if (!string.IsNullOrWhiteSpace(c.UserId) && c.UserId != "string")
                            commenters.Add(c.UserId);
                    }
                }
            }

            post.CommentCount = activeCommentCount;
            post.CommentedUserIds = commenters.ToList();
            post.UpdatedDate = DateTime.UtcNow;

            // Persist updated denorm if you want (optional)
            await _dapr.SaveStateAsync(StoreName, postKey, post, cancellationToken: ct);

            // Publish to search via pub/sub
            var upsert = await IndexCommunityPostToAzureSearch(post, ct);
            var msg = new IndexMessage
            {
                Action = "Upsert",
                Vertical = ConstantValues.IndexNames.ContentCommunityIndex,
                UpsertRequest = upsert
            };
            await _dapr.PublishEventAsync(
                pubsubName: ConstantValues.PubSubName,
                topicName: ConstantValues.PubSubTopics.IndexUpdates,
                data: msg,
                cancellationToken: ct);
        }

        public Task<PaginatedCommunityPostResponseDto> GetAllCommunityPostsAsync(string? categoryId = null, string? search = null, int? page = null, int? pageSize = null, string? sortDirection = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2CommunityPostDto?> GetCommunityPostByIdAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2CommunityPostDto?> GetCommunityPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
