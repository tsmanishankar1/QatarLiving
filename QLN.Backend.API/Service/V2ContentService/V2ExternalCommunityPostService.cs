using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using static QLN.Common.DTO_s.CommunityBo;
using System.Security.Cryptography.X509Certificates;


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
                dto.UpdatedBy = userId;

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
                if (!resp.IsSuccessStatusCode)
                {
                    var errorJson = await resp.Content.ReadAsStringAsync(ct);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch { errorMessage = errorJson; }
                    throw new InvalidDataException(errorMessage);
                }

                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(json) ?? "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community post");
                throw;
            }
        }

        public async Task<PaginatedCommunityPostResponseDto> GetAllCommunityPostsAsync(string? categoryId = null, string? search = null, int? page = null, int? pageSize = null, string? sortDirection = null, CancellationToken ct = default)
        {
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add("search", search);

                if (!string.IsNullOrWhiteSpace(categoryId))
                    queryParams.Add("categoryId", categoryId);

                if (page.HasValue)
                    queryParams.Add("page", page.Value.ToString());

                if (pageSize.HasValue)
                    queryParams.Add("pageSize", pageSize.Value.ToString());

                if (!string.IsNullOrWhiteSpace(sortDirection))
                    queryParams.Add("sortDirection", sortDirection);


                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : string.Empty;

                var url = $"/api/v2/community/getAllPosts{queryString}";
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, InternalAppId, url);

                var resp = await _dapr.InvokeMethodWithResponseAsync(req, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<PaginatedCommunityPostResponseDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new PaginatedCommunityPostResponseDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community posts");
                throw;
            }
        }

        public async Task<ForumCategoryListDto> GetAllForumCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Dapr service invocation to internal MS
                var result = await _dapr.InvokeMethodAsync<ForumCategoryListDto>(
                    HttpMethod.Get,
                    InternalAppId,
                    "api/v2/community/getAllForumCategories",
                    cancellationToken
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking GetAllForumCategories from {AppId}", InternalAppId);
                return new ForumCategoryListDto
                {
                    ForumCategories = new List<ForumCategoryDto>(),

                };
            }

        }
        public async Task<bool> SoftDeleteCommunityPostAsync(Guid postId, string userId, CancellationToken ct = default)
        {
            try
            {
                var url = $"/api/v2/community/deletePostInternal/{postId}";
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Delete, InternalAppId, url);
                req.Content = new StringContent(JsonSerializer.Serialize(userId), Encoding.UTF8, "application/json");

                var resp = await _dapr.InvokeMethodWithResponseAsync(req, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<bool>(json);
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        public async Task<V2CommunityPostDto?> GetCommunityPostByIdAsync(Guid communityId, CancellationToken ct = default)
        {
            try
            {
                var url = $"/api/v2/community/getCommunityPostById/{communityId}";
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, InternalAppId, url);

                var resp = await _dapr.InvokeMethodWithResponseAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch community post with ID {Id}. Status: {Status}", communityId, resp.StatusCode);
                    return null;
                }

                var json = await resp.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<V2CommunityPostDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community post with ID {Id}", communityId);
                throw;
            }
        }       
        public async Task<bool> LikePostForUser(CommunityPostLikeDto dto, CancellationToken ct = default)
        {
            try
            {
                var url = "/api/v2/community/likePost";

                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, InternalAppId, url, dto);
                var resp = await _dapr.InvokeMethodWithResponseAsync(req, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    var jsonDoc = JsonDocument.Parse(json);
                    if (jsonDoc.RootElement.TryGetProperty("status", out var statusElement))
                    {
                        var status = statusElement.GetString();
                        return status == "liked";
                    }
                }

                return false;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error liking/unliking community post");
                throw;
            }
        }
    }
}
