using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Contracts
{
    public interface ICommunityService
    {
        Task<PostListResponse> GetPostsAsync(int? forumId, string order, int page, int pageSize);
        Task<PostDetailsDto> GetPostBySlugAsync(string slug);
        Task<List<MorePostItem>> GetMorePostsAsync();
        Task<List<SelectOption>> GetForumCategoriesAsync();


    }
}
