using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;
using System.Text;
using System.Text.Json;

namespace QLN.DataMigration.Services
{
    public class CommunityPostService : IV2CommunityPostService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<CommunityPostService> _logger;

        public CommunityPostService(
            DaprClient dapr,
            ILogger<CommunityPostService> logger
            )
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateCommunityPostAsync(string userId, V2CommunityPostDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/community/createPostInternal";
                var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                req.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var resp = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    var errorJson = await resp.Content.ReadAsStringAsync(cancellationToken);
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

        public Task<CommunityBo.ForumCategoryListDto> GetAllForumCategoriesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SoftDeleteCommunityPostAsync(Guid postId, string userId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedCommunityPostResponseDto> GetAllCommunityPostsAsync(string? categoryId = null, string? search = null, int? page = null, int? pageSize = null, string? sortDirection = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2CommunityPostDto?> GetCommunityPostByIdAsync(Guid id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LikePostForUser(CommunityPostLikeDto dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task AddCommentToCommunityPostAsync(CommunityCommentDto dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<CommunityCommentListResponse> GetAllCommentsByPostIdAsync(Guid postId, int? page = null, int? perPage = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LikeCommentAsync(LikeCommentsDto dto, string userId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2CommunityPostDto?> GetCommunityPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CommunityCommentApiResponse> SoftDeleteCommunityCommentAsync(Guid postId, Guid commentId, string userId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<CommunityCommentApiResponse> EditCommunityCommentAsync(Guid postId, Guid commentId, string userId, string updatedText, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<CommunityCommentListResponse> GetAllCommentsByPostIdAsync(Guid postId, string? userId, int? page = null, int? perPage = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
