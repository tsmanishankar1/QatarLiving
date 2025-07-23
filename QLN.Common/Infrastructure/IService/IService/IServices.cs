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
        Task<ServicesDto> CreateServiceAd(string userId, ServicesDto dto, CancellationToken cancellationToken = default);
        Task<string> UpdateServiceAd(string userId, ServicesDto dto, CancellationToken cancellationToken = default);
        Task<List<ServicesDto>> GetAllServiceAds(CancellationToken cancellationToken = default);
        Task<ServicesDto?> GetServiceAdById(Guid id, CancellationToken cancellationToken = default);
        Task<string> DeleteServiceAdById(string userId, Guid id, CancellationToken cancellationToken = default);
        Task<ServicesPagedResponse<ServicesDto>> GetServicesByStatusWithPagination(ServiceStatusQuery dto, CancellationToken cancellationToken = default);        
        Task<ServicesDto> PromoteService(PromoteServiceRequest request, CancellationToken ct);
        Task<ServicesDto> FeatureService(FeatureServiceRequest request, CancellationToken ct);
        Task<ServicesDto> RefreshService(RefreshServiceRequest request, CancellationToken ct);
        Task<List<ServicesDto>> ModerateBulkService(BulkModerationRequest request, CancellationToken cancellationToken = default);
    }
}
