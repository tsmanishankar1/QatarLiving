using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text.Json;
using static QLN.Common.DTO_s.CommunityBo;
using System.Text.RegularExpressions;
using QLN.Common.Infrastructure.CustomException;


 
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
                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: ct);
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
                throw new ConflictException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled error occurred during community post creation.");
                throw new InvalidOperationException("An unexpected error occurred while creating the community post. Please try again later.", ex);
            }
        }
        public async Task<PaginatedCommunityPostResponseDto> GetAllCommunityPostsAsync(string? categoryId = null,string? search = null, int? page = null, int? pageSize = null,string? sortDirection = null,CancellationToken ct = default)
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

                        // ✅ Fetch liked user IDs
                        var likeIndexKey = $"like-index-{post.Id}";
                        var likedUsers = await _dapr.GetStateAsync<List<string>>(StoreName, likeIndexKey, cancellationToken: ct) ?? new();
                        post.LikeCount = likedUsers.Count;
                        post.LikedUserIds = likedUsers;

                        // ✅ Fetch comment count + commented user IDs
                        var commentIndexKey = $"comment-index-{post.Id}";
                        var commentIds = await _dapr.GetStateAsync<List<Guid>>(StoreName, commentIndexKey, cancellationToken: ct) ?? new();
                        post.CommentCount = commentIds.Count;

                        var commentedUserIds = new HashSet<string>();

                        foreach (var commentId in commentIds)
                        {
                            var commentKey = $"comment-{post.Id}-{commentId}";
                            var commentState = await _dapr.GetStateAsync<CommunityCommentDto>(StoreName, commentKey, cancellationToken: ct);

                            if (commentState != null && commentState.IsActive && !string.IsNullOrWhiteSpace(commentState.UserId))
                            {
                                commentedUserIds.Add(commentState.UserId);
                            }
                        }

                        post.CommentedUserIds = commentedUserIds.ToList();

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

                if (post == null || !post.IsActive)
                    return null;

                var likeIndexKey = $"like-index-{post.Id}";
                var likedUsers = await _dapr.GetStateAsync<List<string>>(StoreName, likeIndexKey, cancellationToken: ct) ?? new();
                post.LikeCount = likedUsers.Count;
                post.LikedUserIds = likedUsers;

                var commentIndexKey = $"comment-index-{post.Id}";
                var commentIds = await _dapr.GetStateAsync<List<Guid>>(StoreName, commentIndexKey, cancellationToken: ct) ?? new();
                post.CommentCount = commentIds.Count;

                var commentedUserIds = new HashSet<string>();

                foreach (var commentId in commentIds)
                {
                    var commentKey = $"comment-{post.Id}-{commentId}";
                    var commentState = await _dapr.GetStateAsync<CommunityCommentDto>(StoreName, commentKey, cancellationToken: ct);

                    if (commentState != null && commentState.IsActive && !string.IsNullOrWhiteSpace(commentState.UserId))
                    {
                        commentedUserIds.Add(commentState.UserId);
                    }
                }

                post.CommentedUserIds = commentedUserIds.ToList();

                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community post by ID: {PostId}", id);
                throw new InvalidOperationException($"Error retrieving post with ID: {id}", ex);
            }
        }
        private string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            var slug = title.ToLowerInvariant().Trim();
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');
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
        public async Task<CommunityCommentListResponse> GetAllCommentsByPostIdAsync( Guid postId, int? page = null, int? perPage = null, CancellationToken ct = default)
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
                        var comment = JsonSerializer.Deserialize<CommunityCommentDto>(state.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (comment != null && comment.IsActive && comment.CommentId != Guid.Empty)
                        {
                            // Get liked user IDs
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

                // Pagination after filtering
                int currentPage = page ?? 1;
                int itemsPerPage = perPage ?? 10;
                int skip = (currentPage - 1) * itemsPerPage;

                // Group comments by parent
                var grouped = allComments
                    .GroupBy(c => c.ParentCommentId ?? Guid.Empty)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = new List<CommunityCommentItem>();

                // Recursive builder
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
                            Replies = BuildReplies(reply.CommentId)
                        })
                        .ToList();
                }

                // Build paginated root-level comments
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
                            Replies = BuildReplies(parent.CommentId)
                        };

                        result.Add(parentItem);
                    }
                }

                var uniqueCommentedUserIds = allComments
                    .Where(c => !string.IsNullOrWhiteSpace(c.UserId))
                    .Select(c => c.UserId)
                    .Distinct()
                    .ToList();

                return new CommunityCommentListResponse
                {
                    TotalComments = grouped.ContainsKey(Guid.Empty) ? grouped[Guid.Empty].Count : 0,
                    PerPage = itemsPerPage,
                    CurrentPage = currentPage,
                    Comments = result,
                    //CommentedUserIds = uniqueCommentedUserIds
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
        public async Task<V2CommunityPostDto?> GetCommunityPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"[START] Looking for post with slug: {slug}");

                // Get all post keys from index
                var keys = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey, cancellationToken: cancellationToken) ?? new();

                Console.WriteLine($"[INFO] Retrieved {keys.Count} keys from IndexKey = {IndexKey}");

                if (keys.Count == 0)
                {
                    _logger.LogWarning("No community post keys found in the index.");
                    return null;
                }

                // Bulk get all posts
                var items = await _dapr.GetBulkStateAsync(StoreName, keys, parallelism: null, cancellationToken: cancellationToken);

                // Deserialize and filter active posts
                var allPosts = new List<V2CommunityPostDto>();
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Value))
                        continue;

                    try
                    {
                        var post = JsonSerializer.Deserialize<V2CommunityPostDto>(item.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (post == null || !post.IsActive)
                            continue;

                        allPosts.Add(post);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize post for key: {Key}", item.Key);
                        continue;
                    }
                }

                // Find matched post by slug (case insensitive)
                var matchedPost = allPosts.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
                if (matchedPost == null)
                {
                    Console.WriteLine($"[INFO] No post found with slug: {slug}");
                    return null;
                }

                // Initialize collections
                matchedPost.LikedUserIds ??= new();
                matchedPost.CommentedUserIds ??= new();

                // Load likes for matched post
                var likeIndexKey = $"like-index-{matchedPost.Id}";
                var likedUsers = await _dapr.GetStateAsync<List<string>>(StoreName, likeIndexKey, cancellationToken: cancellationToken) ?? new List<string>();
                matchedPost.LikedUserIds = likedUsers
                    .Where(uid => !string.IsNullOrWhiteSpace(uid) && uid != "string")
                    .ToList();
                matchedPost.LikeCount = matchedPost.LikedUserIds.Count;

                Console.WriteLine($"[LIKE] {matchedPost.LikeCount} users liked the post.");

                // Load comments for matched post
                var commentIndexKey = $"comment-index-{matchedPost.Id}";
                var commentIds = await _dapr.GetStateAsync<List<string>>(StoreName, commentIndexKey, cancellationToken: cancellationToken) ?? new List<string>();

                matchedPost.CommentedUserIds.Clear();
                int activeCommentCount = 0;

                if (commentIds.Count > 0)
                {
                    var commentKeys = commentIds.Select(cid => $"comment-{matchedPost.Id}-{cid}").ToList();
                    var commentStates = await _dapr.GetBulkStateAsync(StoreName, commentKeys, null, cancellationToken: cancellationToken);

                    foreach (var state in commentStates)
                    {
                        if (string.IsNullOrWhiteSpace(state.Value))
                            continue;

                        try
                        {
                            var comment = JsonSerializer.Deserialize<CommunityCommentDto>(state.Value, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (comment != null && comment.IsActive &&
                                !string.IsNullOrWhiteSpace(comment.UserId) &&
                                comment.UserId != "string")
                            {
                                matchedPost.CommentedUserIds.Add(comment.UserId);
                                activeCommentCount++;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize comment for key: {Key}", state.Key);
                            Console.WriteLine($"[ERROR] Deserialization failed for comment key: {state.Key}");
                        }
                    }

                    matchedPost.CommentedUserIds = matchedPost.CommentedUserIds
                        .Where(uid => !string.IsNullOrWhiteSpace(uid) && uid != "string")
                        .Distinct()
                        .ToList();
                }

                matchedPost.CommentCount = activeCommentCount;
                Console.WriteLine($"[COMMENT] Total valid unique commenters: {matchedPost.CommentCount}");

                // Load MoreArticles: recent 3 posts excluding current, with full like/comment load
                var recentPosts = allPosts
                    .Where(p => p.Id != matchedPost.Id)
                    .OrderByDescending(p => p.DateCreated)
                    .Take(3)
                    .ToList();

                foreach (var post in recentPosts)
                {
                    post.LikedUserIds ??= new();
                    post.CommentedUserIds ??= new();

                    // Load likes for each recent post
                    var lKey = $"like-index-{post.Id}";
                    var lUsers = await _dapr.GetStateAsync<List<string>>(StoreName, lKey, cancellationToken: cancellationToken) ?? new List<string>();
                    post.LikedUserIds = lUsers
                        .Where(uid => !string.IsNullOrWhiteSpace(uid) && uid != "string")
                        .ToList();
                    post.LikeCount = post.LikedUserIds.Count;

                    // Load comments for each recent post
                    var cKey = $"comment-index-{post.Id}";
                    var cIds = await _dapr.GetStateAsync<List<string>>(StoreName, cKey, cancellationToken: cancellationToken) ?? new List<string>();

                    post.CommentedUserIds.Clear();
                    int postActiveCommentCount = 0;

                    if (cIds.Count > 0)
                    {
                        var cKeys = cIds.Select(cid => $"comment-{post.Id}-{cid}").ToList();
                        var cStates = await _dapr.GetBulkStateAsync(StoreName, cKeys, null, cancellationToken: cancellationToken);

                        foreach (var state in cStates)
                        {
                            if (string.IsNullOrWhiteSpace(state.Value))
                                continue;

                            try
                            {
                                var comment = JsonSerializer.Deserialize<CommunityCommentDto>(state.Value, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                                if (comment != null && comment.IsActive &&
                                    !string.IsNullOrWhiteSpace(comment.UserId) &&
                                    comment.UserId != "string")
                                {
                                    post.CommentedUserIds.Add(comment.UserId);
                                    postActiveCommentCount++;
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "Failed to deserialize comment for key: {Key}", state.Key);
                            }
                        }

                        post.CommentedUserIds = post.CommentedUserIds
                            .Where(uid => !string.IsNullOrWhiteSpace(uid) && uid != "string")
                            .Distinct()
                            .ToList();
                    }

                    post.CommentCount = postActiveCommentCount;
                }

                matchedPost.MoreArticles = recentPosts;

                Console.WriteLine($"[INFO] Found {matchedPost.MoreArticles.Count} more articles.");

                return matchedPost;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving community post by slug: {Slug}", slug);
                Console.WriteLine($"[FATAL] Error fetching post by slug: {slug}, Exception: {ex.Message}");
                throw new InvalidOperationException($"Error fetching community post by slug: {slug}", ex);
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

                // 🔁 Soft delete replies
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








    }
}
