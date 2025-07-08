using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static QLN.Common.DTO_s.CommunityBo;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace QLN.Content.MS.Service.CommunityInternalService
{
    public class V2InternalCommunityPostService : IV2CommunityPostService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2InternalCommunityPostService> _logger;

        private static string GetKey(Guid id) => $"community-{id}";
        private const string StoreName = "contentstatestore";
        private const string IndexKey = "community-index";

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
            dto.Slug = GenerateSlug(dto.Title);



            var key = GetKey(dto.Id);

            try
            {
                var existing = await _dapr.GetStateAsync<object>(StoreName, key, cancellationToken: ct);
                if (existing != null)
                    throw new InvalidOperationException($"Community post with key {key} already exists.");

                // Save post
                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);

                // Update index
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct) ?? new();
                if (!index.Contains(key))
                {
                    index.Add(key);
                    await _dapr.SaveStateAsync(StoreName, IndexKey, index, cancellationToken: ct);
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
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during community post creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the community post. Please try again later.", ex);
            }
        }
        public async Task<PaginatedCommunityPostResponseDto> GetAllCommunityPostsAsync(string? categoryId = null, string? search = null, int? page = null, int? pageSize = null, string? sortDirection = null, CancellationToken ct = default)
        {
            _logger.LogInformation("Starting retrieval of all community posts...");

            try
            {
                int currentPage = page ?? 1;
                int currentPageSize = pageSize ?? 12;

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct) ?? new();
                _logger.LogInformation("Retrieved {Count} community post keys from index.", index.Count);

                if (index.Count == 0)
                {
                    _logger.LogWarning("No community post keys found in the index.");
                    return new PaginatedCommunityPostResponseDto();
                }

                var filteredPosts = new List<V2CommunityPostDto>();

                foreach (var key in index)
                {
                    try
                    {
                        var post = await _dapr.GetStateAsync<V2CommunityPostDto>(StoreName, key, cancellationToken: ct);

                        if (post == null || post.Id == Guid.Empty || string.IsNullOrWhiteSpace(post.Title))
                            continue;

                        if (!post.IsActive)
                            continue;

                        if (!string.IsNullOrWhiteSpace(categoryId) &&
                            !string.Equals(post.CategoryId?.Trim(), categoryId.Trim(), StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            var normalizedTitle = post.Title?.Trim().ToLowerInvariant() ?? "";
                            var normalizedSearch = search.Trim().ToLowerInvariant();
                            if (!normalizedTitle.Contains(normalizedSearch))
                                continue;
                        }

                        var likeIndexKey = $"like-index-{post.Id}";
                        var likedUsers = await _dapr.GetStateAsync<List<string>>(StoreName, likeIndexKey, cancellationToken: ct);
                        post.LikeCount = likedUsers?.Count ?? 0;

                        var commentIndexKey = $"comment-index-{post.Id}";
                        var commentList = await _dapr.GetStateAsync<List<Guid>>(StoreName, commentIndexKey);
                        post.CommentCount = commentList?.Count ?? 0;

                        filteredPosts.Add(post);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing community post from key: {Key}", key);
                    }
                }

                var sort = (sortDirection ?? "desc").Trim().ToLowerInvariant();
                filteredPosts = sort == "asc"
                    ? filteredPosts.OrderBy(p => p.DateCreated).ToList()
                    : filteredPosts.OrderByDescending(p => p.DateCreated).ToList();

                int total = filteredPosts.Count;

                var pagedPosts = filteredPosts
                    .Skip((currentPage - 1) * currentPageSize)
                    .Take(currentPageSize)
                    .ToList();

                _logger.LogInformation("Completed retrieval. Total filtered posts: {Total}, Page {Page} with size {PageSize}", total, currentPage, currentPageSize);

                return new PaginatedCommunityPostResponseDto
                {
                    Total = total,
                    Items = pagedPosts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve community posts.");
                throw new InvalidOperationException("An unexpected error occurred while retrieving community posts.", ex);
            }
        }

        public async Task<V2CommunityPostDto?> GetCommunityPostByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var key = $"community-{id}";
                var post = await _dapr.GetStateAsync<V2CommunityPostDto>(StoreName, key, cancellationToken: ct);
                return (post != null && post.IsActive) ? post : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community posts");
                throw;
            }
        }

        private string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            var slug = title.Trim().ToLower()
                             .Replace(" ", "-")
                             .Replace("--", "-")
                             .Replace("and", "-")
                             .Replace("of", "-")
                             .Replace("the", "-");
            return slug;
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
                    await _dapr.SaveStateAsync(StoreName, indexKey, userIndex);

                    _logger.LogInformation("User {UserId} unliked post {PostId}", dto.UserId, dto.CommunityPostId);
                    return false; // false means unliked
                }

                // Not liked → add like
                dto.LikePostId = Guid.NewGuid();
                dto.LikedDate = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);

                if (!userIndex.Contains(dto.UserId))
                    userIndex.Add(dto.UserId);

                await _dapr.SaveStateAsync(StoreName, indexKey, userIndex, cancellationToken: ct);

                _logger.LogInformation("User {UserId} liked post {PostId}", dto.UserId, dto.CommunityPostId);
                return true; // true means liked
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community like posts");
                throw;
            }
        }

        public async Task AddCommentToCommunityPostAsync(CommunityCommentDto dto, CancellationToken ct = default)
        {
            var key = $"comment-{dto.CommunityPostId}-{dto.CommentId}";
            var indexKey = $"comment-index-{dto.CommunityPostId}";

            try
            {
                await _dapr.SaveStateAsync(StoreName, key, dto);

                // Update the index list
                var index = await _dapr.GetStateAsync<List<Guid>>(StoreName, indexKey) ?? new();
                index.Add(dto.CommentId);

                await _dapr.SaveStateAsync(StoreName, indexKey, index);
                _logger.LogInformation("Comment added to community post {PostId}", dto.CommunityPostId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to community post {PostId}", dto.CommunityPostId);
                throw;
            }
        }

        public async Task<CommunityCommentListResponse> GetCommentsByPostIdAsync(
            Guid postId,
            int? page = null,
            int? perPage = null,
            CancellationToken ct = default)
        {
            var indexKey = $"comment-index-{postId}";
            // Defensive: Deduplicate if any accidental dups in index
            var index = (await _dapr.GetStateAsync<List<Guid>>(StoreName, indexKey, cancellationToken: ct)
                ?? new List<Guid>()).Distinct().ToList();

            int total = index.Count;
            int currentPage = page ?? 1;
            int itemsPerPage = perPage ?? 10;
            int skip = (currentPage - 1) * itemsPerPage;

            // Pagination
            var pagedCommentIds = index.Skip(skip).Take(itemsPerPage).ToList();
            var commentKeys = pagedCommentIds
                .Select(id => $"comment-{postId}-{id}")
                .ToList();

            var commentStates = await _dapr.GetBulkStateAsync(StoreName, commentKeys, null);
            var comments = new List<CommunityCommentDto>();

            foreach (var state in commentStates)
            {
                if (string.IsNullOrWhiteSpace(state.Value))
                {
                    _logger.LogWarning("Orphaned comment ID in index for post {PostId}: {Key}", postId, state.Key);
                    continue;
                }

                CommunityCommentDto? comment = null;
                try
                {
                    comment = JsonSerializer.Deserialize<CommunityCommentDto>(state.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize comment: {Key} Value: {Value}", state.Key, state.Value);
                    continue;
                }

                if (comment == null || !comment.IsActive)
                    continue;

                comments.Add(comment);
            }

            // Optional: update index if orphans found (keeps index clean over time)
            if (comments.Count < pagedCommentIds.Count)
            {
                var cleanedIndex = index.Where(id => comments.Any(c => c.CommentId == id)).ToList();
                await _dapr.SaveStateAsync(StoreName, indexKey, cleanedIndex, cancellationToken: ct);
            }

            comments = comments.OrderByDescending(c => c.CommentedAt).ToList();

            return new CommunityCommentListResponse
            {
                TotalComments = total,
                PerPage = itemsPerPage,
                CurrentPage = currentPage,
                Comments = comments
            };
        }
        public async Task<bool> LikeCommentAsync(Guid commentId, string userId, Guid communityPostId, CancellationToken ct = default)
        {
            var key = $"comment-like-{commentId}-{userId}";
            var indexKey = $"comment-like-index-{commentId}";

            try
            {
                var existing = await _dapr.GetStateAsync<string>(StoreName, key);
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    await _dapr.DeleteStateAsync(StoreName, key);
                    index.Remove(userId);
                    await _dapr.SaveStateAsync(StoreName, indexKey, index);

                    _logger.LogInformation("User {UserId} unliked comment {CommentId}", userId, commentId);
                    return false;
                }

                await _dapr.SaveStateAsync(StoreName, key, userId);

                if (!index.Contains(userId))
                    index.Add(userId);

                await _dapr.SaveStateAsync(StoreName, indexKey, index);

                _logger.LogInformation("User {UserId} liked comment {CommentId}", userId, commentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing comment like for {CommentId}", commentId);
                throw;
            }
        }

        public async Task<V2CommunityPostDto?> GetCommunityPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey) ?? new();

                // Bulk fetch for all post keys
                var items = await _dapr.GetBulkStateAsync(StoreName, keys, parallelism: null, cancellationToken: cancellationToken);

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Value))
                        continue;

                    var post = JsonSerializer.Deserialize<V2CommunityPostDto>(item.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (post != null && string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase) && post.IsActive)
                    {
                        return post;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving community post by slug: {Slug}", slug);
                throw;
            }
        }

        public async Task<bool> DislikeCommentAsync(Guid commentId, string userId, Guid communityPostId, CancellationToken ct = default)
        {
            var key = $"comment-dislike-{commentId}-{userId}";
            var indexKey = $"comment-dislike-index-{commentId}";

            try
            {
                var existing = await _dapr.GetStateAsync<string>(StoreName, key, cancellationToken: ct);
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey, cancellationToken: ct) ?? new();

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    await _dapr.DeleteStateAsync(StoreName, key, cancellationToken: ct);
                    index.Remove(userId);
                    await _dapr.SaveStateAsync(StoreName, indexKey, index, cancellationToken: ct);

                    _logger.LogInformation("User {UserId} removed dislike from comment {CommentId}", userId, commentId);
                    return false;
                }

                await _dapr.SaveStateAsync(StoreName, key, userId, cancellationToken: ct);

                if (!index.Contains(userId))
                    index.Add(userId);

                await _dapr.SaveStateAsync(StoreName, indexKey, index, cancellationToken: ct);

                _logger.LogInformation("User {UserId} disliked comment {CommentId}", userId, commentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dislike for comment {CommentId}", commentId);
                throw;
            }
        }

    }
}
