using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Services.Interface
{
    public interface IBannerService
    {
        Task<BannerResponse?> GetBannerAsync();
    }
}
