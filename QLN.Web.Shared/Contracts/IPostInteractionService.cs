using QLN.Web.Shared.Model;

namespace QLN.Web.Shared.Contracts
{
    public interface IPostInteractionService
    {
        Task<bool> LikeOrUnlikeAsync(PostInteractionRequest request);

    }
}
