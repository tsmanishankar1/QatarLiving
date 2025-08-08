using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IService
{
    public interface IServices
    {
        Task<string> CreateCategory(CategoryDto dto, CancellationToken cancellationToken);
        Task<string> UpdateCategory(CategoryDto dto, CancellationToken cancellationToken = default);
        Task<List<CategoryDto>> GetAllCategories(CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetCategoryById(long id, CancellationToken cancellationToken = default);
        Task<string> CreateServiceAd(string uid, string userName, ServiceDto dto, CancellationToken cancellationToken = default);
        Task<string> UpdateServiceAd(string userId, Services dto, CancellationToken cancellationToken = default);
        Task<Services?> GetServiceAdById(long id, CancellationToken cancellationToken = default);
        Task<ServicesPagedResponse<QLN.Common.Infrastructure.Model.Services>> GetAllServicesWithPagination(BasePaginationQuery? dto, CancellationToken cancellationToken = default);
        Task<string> DeleteServiceAdById(string userId, long id, CancellationToken cancellationToken = default);
        Task<Services> PromoteService(PromoteServiceRequest request, CancellationToken ct);
        Task<Services> FeatureService(FeatureServiceRequest request, CancellationToken ct);
        Task<Services> RefreshService(RefreshServiceRequest request, CancellationToken ct);
        Task<Services> PublishService(long id, CancellationToken ct);
        Task<List<Services>> ModerateBulkService(BulkModerationRequest request, CancellationToken cancellationToken = default);
    }
}
