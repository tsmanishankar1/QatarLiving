using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;


namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2BannerService
    {

        Task<string> CreateBannerAsync(string uid, V2CreateBannerDto dto, CancellationToken cancellationToken = default);
        Task<string> EditBannerAsync(string uid, V2BannerDto dto, CancellationToken cancellationToken = default);
        Task<string> DeleteBannerAsync(string uid, Guid bannerId, CancellationToken cancellationToken = default);
        Task<V2BannerDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<string> CreateBannerTypeAsync(V2BannerTypeDto dto, CancellationToken cancellationToken = default);
        Task<List<V2BannerTypeDto>> GetAllBannerTypesAsync(CancellationToken cancellationToken = default);
        Task<List<V2BannerTypeDto>?> GetBannerTypesByFilterAsync(Vertical verticalId,SubVertical? subVerticalId,Guid pageId,CancellationToken cancellationToken);
        Task<List<V2BannerTypeDto>> GetBannerTypesWithBannersByStatusAsync(Vertical? verticalId,bool? status,CancellationToken cancellationToken);
        Task<string> ReorderAsync(Vertical verticalId, SubVertical? subVerticalId, Guid pageId, List<Guid> banners, CancellationToken cancellationToken = default);
    }
       
}
