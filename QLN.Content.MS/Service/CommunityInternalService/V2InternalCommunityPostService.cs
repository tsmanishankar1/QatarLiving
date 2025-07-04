using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
    }
}
