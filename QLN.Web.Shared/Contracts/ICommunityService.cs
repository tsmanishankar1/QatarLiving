using QLN.Web.Shared.Model;

namespace QLN.Web.Shared.Contracts
{
    public interface ICommunityService
    {
        Task<List<PostListDto>> GetPostsAsync(int forumId, string order, int page, int pageSize);
        Task<PostDetailsDto> GetPostBySlugAsync(string slug);

    }
}
