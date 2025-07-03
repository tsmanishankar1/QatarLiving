using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;

namespace QLN.Content.MS.Service.CommunityInternalService
{
    public class V2InternalCommunityPostService : IV2CommunityPostService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2InternalCommunityPostService> _logger;
        private const string StoreName = "contentstatestore";
        private const string IndexKey = "community-posts-index";

        public V2InternalCommunityPostService(DaprClient dapr, ILogger<V2InternalCommunityPostService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateCommunityPostAsync(string userId, V2CommunityPostDto dto, CancellationToken ct = default)
        {
            try
            {
                dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
                dto.UpdatedBy = userId;
                dto.UpdatedDate = DateTime.UtcNow;
                dto.DateCreated = DateTime.UtcNow;
                dto.Slug = GenerateSlug(dto.Title);

                var key = dto.Id.ToString();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create community post");
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