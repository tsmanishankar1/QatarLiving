using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IService
{
    public interface IServices
    {
        Task<string> CreateCategory(ServicesCategory dto, CancellationToken cancellationToken = default);
        Task<string> UpdateCategory(ServicesCategory dto, CancellationToken cancellationToken = default);
        Task<List<ServicesCategory>> GetAllCategories(CancellationToken cancellationToken = default);
        Task<ServicesCategory?> GetCategoryById(Guid id, CancellationToken cancellationToken = default);
        Task<string> CreateServiceAd(string uid, string userName, ServiceDto dto, CancellationToken cancellationToken = default);
        Task<string> UpdateServiceAd(string userId, ServicesModel dto, CancellationToken cancellationToken = default);
        Task<List<ServicesModel>> GetAllServiceAds(CancellationToken cancellationToken = default);
        Task<ServicesModel?> GetServiceAdById(Guid id, CancellationToken cancellationToken = default);
        Task<string> DeleteServiceAdById(string userId, Guid id, CancellationToken cancellationToken = default);
        Task<ServicesPagedResponse<ServicesModel>> GetServicesByStatusWithPagination(ServiceStatusQuery dto, CancellationToken cancellationToken = default);        
        Task<ServicesModel> PromoteService(PromoteServiceRequest request, CancellationToken ct);
        Task<ServicesModel> FeatureService(FeatureServiceRequest request, CancellationToken ct);
        Task<ServicesModel> RefreshService(RefreshServiceRequest request, CancellationToken ct);
        Task<ServicesModel> PublishService(Guid id, CancellationToken ct);
        Task<List<ServicesModel>> ModerateBulkService(BulkModerationRequest request, CancellationToken cancellationToken = default);
    }
}
