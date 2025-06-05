using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Services.Interface
{
    public interface ISimpleMemoryCache
    {
        Task<BannerResponse?> GetBannerAsync();
        Task<ContentsDailyPageResponse?> GetContentLandingAsync();
    }
}
