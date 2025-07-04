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
        public async Task<List<V2CommunityPostDto>> GetAllCommunityPostsAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Starting retrieval of all community posts...");

            try
            {
                _logger.LogDebug("Fetching community post index from Dapr state store: {StoreName}, IndexKey: {IndexKey}", StoreName, IndexKey);
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey, cancellationToken: ct) ?? new();

                _logger.LogInformation("Retrieved {Count} community post keys from index.", index.Count);

                if (index.Count == 0)
                {
                    _logger.LogWarning("No community post keys found in the index.");
                    return new List<V2CommunityPostDto>();
                }

                var posts = new List<V2CommunityPostDto>();

                foreach (var key in index)
                {
                    try
                    {
                        _logger.LogDebug("Attempting to retrieve post for key: {Key}", key);
                        var post = await _dapr.GetStateAsync<V2CommunityPostDto>(StoreName, key, cancellationToken: ct);

                        if (post == null)
                        {
                            _logger.LogWarning("Post is null for key: {Key}", key);
                            continue;
                        }

                        if (post.Id == Guid.Empty || string.IsNullOrWhiteSpace(post.Title))
                        {
                            _logger.LogWarning("Post has missing required fields. Skipping. Key: {Key}, Id: {Id}, Title: {Title}", key, post.Id, post.Title);
                            continue;
                        }

                        _logger.LogInformation("Successfully retrieved post: {Title} (ID: {Id})", post.Title, post.Id);
                        posts.Add(post);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving or deserializing community post for key: {Key}", key);
                    }
                }

                _logger.LogInformation("Completed retrieval. Total valid posts: {Count}", posts.Count);
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve community posts.");
                throw new InvalidOperationException("An unexpected error occurred while retrieving community posts.", ex);
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
