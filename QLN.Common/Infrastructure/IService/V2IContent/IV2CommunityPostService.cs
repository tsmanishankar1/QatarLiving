using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.CommunityBo;
using static QLN.Common.DTO_s.LocationDto;

namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2CommunityPostService
    {
        Task<string> CreateCommunityPostAsync(string userId, V2CommunityPostDto dto, CancellationToken cancellationToken = default);
        Task<ForumCategoryListDto> GetAllForumCategoriesAsync(CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteCommunityPostAsync(Guid postId, string userId, CancellationToken ct = default);
        Task<PaginatedCommunityPostResponseDto> GetAllCommunityPostsAsync(string? categoryId = null, string? search = null, int? page = null, int? pageSize = null, string? sortDirection = null, CancellationToken ct = default);
        Task<V2CommunityPostDto?> GetCommunityPostByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> LikePostForUser(CommunityPostLikeDto dto, CancellationToken ct = default);
        Task AddCommentToCommunityPostAsync(CommunityCommentDto dto, CancellationToken ct = default);
        Task<List<CommunityCommentDto>> GetAllCommentsByPostIdAsync(Guid postId, CancellationToken ct = default);
        Task<bool> LikeCommentAsync(Guid commentId, string userId, Guid communityPostId, CancellationToken ct = default);
    }
}
