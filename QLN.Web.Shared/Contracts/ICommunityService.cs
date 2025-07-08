using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Contracts
{
    public interface ICommunityService
    {
        Task<(List<PostListDto> Posts, int TotalCount)> GetPostsAsync(int? forumId, string order, int page, int pageSize);
        Task<PostDetailsDto> GetPostBySlugAsync(string slug);
        Task<List<MorePostItem>> GetMorePostsAsync();
        Task<List<SelectOption>> GetForumCategoriesAsync();
        Task<bool> PostCommentAsync(CommentPostRequest request);
        Task<PaginatedCommentResponse> GetCommentsByPostIdAsync(int nid, int page , int pageSize );

        Task<(List<CommunityPostModel> Posts, int TotalCount)> GetCommunityPostList(int? categoryId, string? search, int? page, int? pageSize, string? sortDirection);
        Task<CommunityPostModel> GetCommunityPostDetail(string slug);
        Task<List<CommunityCategoryModel>> GetCommunityCategoriesAsync();
        Task<bool> CreateCommunityPostAsync(CreateCommunityPostDto dto);
        Task<bool> PostCommentAsyncV2(CommentPostRequestDto dto);
        Task<List<CommentModelV2>> GetCommentsByPostIdAsyncV2(string postId);
        Task<bool> ReportCommunityPostAsync(string postId);
        Task<bool> ReportCommentAsync(string postId, string commentId);
        Task<bool> LikeCommunityPostAsync(string postId);



    }
}
