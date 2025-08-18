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
        Task<List<CategoryDto>> GetAllCategories(string? vertical, string? subVertical, CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetCategoryById(long id, CancellationToken cancellationToken = default);
        Task<string> CreateServiceAd(string uid, string userName, string subscriptionId, ServiceDto dto, CancellationToken cancellationToken = default);
        Task<string> UpdateServiceAd(string userId, Services dto, CancellationToken cancellationToken = default);
        Task<Services?> GetServiceAdById(long id, CancellationToken cancellationToken = default);
        Task<Services?> GetServiceAdBySlug(string? slug, CancellationToken cancellationToken = default);
        Task<ServicesPagedResponse<Services>> GetAllServicesWithPagination(BasePaginationQuery? dto, CancellationToken cancellationToken = default);
        Task<string> DeleteServiceAdById(string userId, long id, CancellationToken cancellationToken = default);
        Task<Services> PromoteService(PromoteServiceRequest request, string? uid, string? subscriptionId, CancellationToken ct);
        Task<Services> FeatureService(FeatureServiceRequest request, string? uid, string? subscriptionId, CancellationToken ct = default);
        Task<Services> RefreshService(RefreshServiceRequest request, string? uid, string? subscriptionId,  CancellationToken ct);
        Task<Services> PublishService(PublishServiceRequest request, string? uid, string? subscriptionId, CancellationToken ct);
        Task<BulkAdActionResponseitems> ModerateBulkService(BulkModerationRequest request, string userId, string subscriptionId, DateTime? expiryDate, CancellationToken cancellationToken = default);
        Task<SubscriptionBudgetDto> GetSubscriptionBudgetsAsync(Guid subscriptionId,CancellationToken cancellationToken = default);
        Task<SubscriptionBudgetDto> GetSubscriptionBudgetsAsyncBySubVertical(
        Guid subscriptionIdFromToken,
        int verticalId,
        int? subVerticalId,
        CancellationToken cancellationToken = default);
    }
}
