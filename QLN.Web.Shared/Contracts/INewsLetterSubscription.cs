using QLN.Web.Shared.Model;

namespace QLN.Web.Shared.Contracts
{
    public interface INewsLetterSubscription
    {
        Task<bool> SubscribeAsync(NewsLetterSubscriptionModel model);

    }
}
