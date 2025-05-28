using QLN.Web.Shared.Model;

namespace QLN.Web.Shared.Contracts
{
    public interface ICommunityService
    {
        Task<IEnumerable<PostModel>> GetAllAsync();

    }
}
