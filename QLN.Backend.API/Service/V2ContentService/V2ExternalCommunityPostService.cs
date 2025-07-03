using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalCommunityPostService : IV2CommunityPostService
    {
        private readonly DaprClient _dapr;
        private readonly IFileStorageBlobService _blobStorage;
        private readonly ILogger<V2ExternalCommunityPostService> _logger;

        private const string InternalAppId = "qln-content-ms";
        private const string BlobContainer = "community-images";

        public V2ExternalCommunityPostService(
            DaprClient dapr,
            ILogger<V2ExternalCommunityPostService> logger,
            IFileStorageBlobService blobStorage)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
        }

        public async Task<string> CreateCommunityPostAsync(string userId, V2CommunityPostDto dto, CancellationToken ct = default)
        {
            try
            {
                dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
                dto.DateCreated = DateTime.UtcNow;
                dto.UpdatedDate = DateTime.UtcNow;
                dto.UserName = userId;

                // Upload image if present
                if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                {
                    var (ext, base64) = Base64ImageHelper.ParseBase64Image(dto.ImageBase64);
                    var fileName = $"{dto.Title}_{dto.Id}.{ext}";
                    dto.ImageUrl = await _blobStorage.SaveBase64File(base64, fileName, BlobContainer, ct);
                    dto.ImageBase64 = null; // Security: Do not persist base64
                }

                var url = "/api/v2/community/createPostInternal";
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, InternalAppId, url);
                req.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var resp = await _dapr.InvokeMethodWithResponseAsync(req, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(json) ?? "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community post");
                throw;
            }
        }
    }

}