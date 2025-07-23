using QLN.Common.DTO_s;


namespace QLN.Common.Infrastructure.IService.IServiceBoService
{
    public interface  IServicesBoService
    {
        Task<List<ServiceAdSummaryDto>> GetAllServiceBoAds(CancellationToken cancellationToken = default);
    }
}
