using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;
using static QLN.Common.DTO_s.CommunityBo;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalCommunityPostService : IV2CommunityPostService
    {
        private readonly DaprClient _dapr;
        private readonly IFileStorageBlobService _blobStorage;
        private readonly ILogger<V2ExternalCommunityPostService> _logger;
        private readonly ISearchService _search;

        private const string InternalAppId = "qln-content-ms";
        private const string BlobContainer = "content-images";

        public V2ExternalCommunityPostService(
            DaprClient dapr,
            ILogger<V2ExternalCommunityPostService> logger,
            IFileStorageBlobService blobStorage,
            ISearchService search)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
            _search = search;
        }

        private static string Escape(string s) => s.Replace("'", "''");

        private static V2CommunityPostDto MapIndexToDto(ContentCommunityIndex i)
        {
            Guid.TryParse(i.Id, out var id);
            return new V2CommunityPostDto
            {
                Id = id,
                UserName = i.UserName,
                Title = i.Title,
                Slug = i.Slug,
                Description = i.Description,
                Category = i.Category,
                CategoryId = i.CategoryId,
                CommentedUserIds = (i.CommentedUserIds ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x) && x != "string")
                    .Distinct()
                    .ToList(),
                CommentCount = i.CommentCount,
                LikedUserIds = (i.LikedUserIds ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x) && x != "string")
                    .Distinct()
                    .ToList(),
                LikeCount = i.LikeCount,
                ImageUrl = i.ImageUrl,
                UserId = i.UserId,
                IsActive = i.IsActive,
                DateCreated = i.DateCreated,
                UpdatedBy = i.UpdatedBy,
                UpdatedDate = i.UpdatedDate
            };
        }

        public async Task<string> CreateCommunityPostAsync(string userId, V2CommunityPostDto dto, CancellationToken ct = default)
        {
            try
            {
                dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
                dto.DateCreated = DateTime.UtcNow;
                dto.UpdatedDate = DateTime.UtcNow;
                dto.UserId = userId;
                dto.UpdatedBy = userId;

                if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
                {
                    var (ext, base64) = Base64ImageHelper.ParseBase64Image(dto.ImageBase64);
                    var fileName = $"{dto.Title}_{dto.Id}.{ext}";
                    dto.ImageUrl = await _blobStorage.SaveBase64File(base64, fileName, BlobContainer, ct);
                    dto.ImageBase64 = null; // don't persist base64
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

                var json = await resp.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<string>(json) ?? "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community post");
                throw;
            }
        }

        public async Task<PaginatedCommunityPostResponseDto> GetAllCommunityPostsAsync(
            string? categoryId = null,
            string? search = null,
            int? page = null,
            int? pageSize = null,
            string? sortDirection = null,
            CancellationToken ct = default)
        {
            var currentPage = Math.Max(1, page ?? 1);
            var perPage = Math.Clamp(pageSize ?? 12, 1, 100);

            var filters = new List<string> { "IsActive eq true" };
            if (!string.IsNullOrWhiteSpace(categoryId))
            {
                filters.Add($"CategoryId eq '{Escape(categoryId)}'");
            }

            var orderBy = (sortDirection ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? "DateCreated asc"
                : "DateCreated desc";

            var text = string.IsNullOrWhiteSpace(search) ? "*" : search!.Trim();

            var req = new RawSearchRequest
            {
                Filter = string.Join(" and ", filters),
                OrderBy = orderBy,
                Top = perPage,
                Skip = (currentPage - 1) * perPage,
                Text = text,
                IncludeTotalCount = true
            };

            var res = await _search.SearchRawAsync<ContentCommunityIndex>(
                ConstantValues.IndexNames.ContentCommunityIndex, req, ct);

            var items = (res.Items ?? new List<ContentCommunityIndex>())
                .Select(MapIndexToDto)
                .ToList();

            return new PaginatedCommunityPostResponseDto
            {
                Total = (int)res.TotalCount,
                Items = items
            };
        }

        public async Task<ForumCategoryListDto> GetAllForumCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<ForumCategoryListDto>(
                    HttpMethod.Get,
                    InternalAppId,
                    "api/v2/community/getAllForumCategories",
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking GetAllForumCategories from {AppId}", InternalAppId);
                return new ForumCategoryListDto { ForumCategories = new List<ForumCategoryDto>() };
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

                var json = await resp.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<bool>(json);
            }
            catch
            {
                return false;
            }
        }

        public async Task<V2CommunityPostDto?> GetCommunityPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var req = new RawSearchRequest
            {
                Filter = $"IsActive eq true and Slug eq '{Escape(slug)}'",
                OrderBy = "DateCreated desc",
                Top = 1,
                Skip = 0,
                Text = "*",
                IncludeTotalCount = false
            };

            var res = await _search.SearchRawAsync<ContentCommunityIndex>(
                ConstantValues.IndexNames.ContentCommunityIndex, req, cancellationToken);

            var current = res.Items?.FirstOrDefault();
            if (current is null) return null;

            var dto = MapIndexToDto(current);

            var moreReq = new RawSearchRequest
            {
                Filter = $"IsActive eq true and Id ne '{current.Id}'",
                OrderBy = "DateCreated desc",
                Top = 3,
                Skip = 0,
                Text = "*",
                IncludeTotalCount = false
            };

            var more = await _search.SearchRawAsync<ContentCommunityIndex>(
                ConstantValues.IndexNames.ContentCommunityIndex, moreReq, cancellationToken);

            dto.MoreArticles = (more.Items ?? new List<ContentCommunityIndex>())
                .Select(MapIndexToDto)
                .ToList();

            return dto;
        }

        public async Task<V2CommunityPostDto?> GetCommunityPostByIdAsync(Guid communityId, CancellationToken ct = default)
        {
            var doc = await _search.GetByIdAsync<ContentCommunityIndex>(
                ConstantValues.IndexNames.ContentCommunityIndex, communityId.ToString());

            if (doc is null || !doc.IsActive) return null;
            return MapIndexToDto(doc);
        }

        public async Task<bool> LikePostForUser(CommunityPostLikeDto dto, CancellationToken ct = default)
        {
            try
            {
                var url = "/api/v2/community/likePost";
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, InternalAppId, url, dto);
                var resp = await _dapr.InvokeMethodWithResponseAsync(req, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync(ct);
                if (!string.IsNullOrEmpty(json))
                {
                    using var jsonDoc = JsonDocument.Parse(json);
                    if (jsonDoc.RootElement.TryGetProperty("status", out var statusElement))
                    {
                        return string.Equals(statusElement.GetString(), "liked", StringComparison.OrdinalIgnoreCase);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking/unliking community post");
                throw;
            }
        }

        public async Task AddCommentToCommunityPostAsync(CommunityCommentDto dto, CancellationToken ct = default)
        {
            try
            {
                var url = "/api/v2/community/addComment";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, InternalAppId, url, dto);
                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully invoked add comment for post {PostId} via Dapr", dto.CommunityPostId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to community post {PostId} via Dapr", dto.CommunityPostId);
                throw;
            }
        }

        public async Task<CommunityCommentListResponse> GetAllCommentsByPostIdAsync(Guid postId, string? userId, int? page = null, int? perPage = null, CancellationToken ct = default)
        {
            try
            {
                var query = $"?userId={userId}&page={page ?? 1}&perPage={perPage ?? 10}";
                var url = $"/api/v2/community/getCommentsByPost/{postId}{query}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, InternalAppId, url);
                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);

                return JsonSerializer.Deserialize<CommunityCommentListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new CommunityCommentListResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for post {PostId} via Dapr", postId);
                throw;
            }
        }

        public async Task<bool> LikeCommentAsync(LikeCommentsDto likeCommentsDto, string userId, CancellationToken ct = default)
        {
            try
            {
                var url = $"/api/v2/community/likeCommentInternal?userId={userId}";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, InternalAppId, url);
                var jsonContent = JsonSerializer.Serialize(likeCommentsDto);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                if (!string.IsNullOrEmpty(json))
                {
                    using var jsonDoc = JsonDocument.Parse(json);
                    if (jsonDoc.RootElement.TryGetProperty("status", out var statusElement))
                        return string.Equals(statusElement.GetString(), "liked", StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking LikeCommentAsync via Dapr");
                throw;
            }
        }

        public async Task<CommunityCommentApiResponse> SoftDeleteCommunityCommentAsync(Guid postId, Guid commentId, string userId, CancellationToken ct = default)
        {
            try
            {
                var encodedUserId = Uri.EscapeDataString(userId);
                var url = $"/api/v2/community/comments/delete/byid/{postId}/{commentId}?userId={encodedUserId}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, InternalAppId, url);
                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Soft delete failed: StatusCode={Status}, Content={Content}", response.StatusCode, error);

                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = $"Soft delete failed: {response.ReasonPhrase}"
                    };
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<CommunityCommentApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new CommunityCommentApiResponse { Status = "failed", Message = "No content received" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Soft delete exception");
                return new CommunityCommentApiResponse { Status = "failed", Message = $"Exception occurred: {ex.Message}" };
            }
        }

        public async Task<CommunityCommentApiResponse> EditCommunityCommentAsync(Guid postId, Guid commentId, string userId, string updatedText, CancellationToken ct = default)
        {
            try
            {
                var encodedUserId = Uri.EscapeDataString(userId);
                var url = $"/api/v2/community/comments/edit/byid/{postId}/{commentId}?userId={encodedUserId}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, InternalAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(updatedText), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Internal edit call failed with status {StatusCode}. Error: {Error}", response.StatusCode, errorContent);

                    return new CommunityCommentApiResponse
                    {
                        Status = "failed",
                        Message = $"Edit failed: {response.StatusCode}"
                    };
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<CommunityCommentApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new CommunityCommentApiResponse { Status = "failed", Message = "Empty response received from internal service" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit community comment {CommentId} for post {PostId} by user {UserId}", commentId, postId, userId);
                return new CommunityCommentApiResponse { Status = "failed", Message = "Edit request failed" };
            }
        }

        public Task<string> BulkMigrateCommunityPostsAsync(List<V2CommunityPostDto> posts, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<string> MigrateCommunityPostAsync(V2CommunityPostDto post, CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}
