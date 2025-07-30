using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;


namespace QLN.Common.Infrastructure.IService.IServiceBoService
{
    public interface  IServicesBoService
    {
        Task<PaginatedResult<ServiceAdSummaryDto>> GetAllServiceBoAds(
         string? sortBy = "CreationDate",
         string? search = null,
         DateTime? fromDate = null,
         DateTime? toDate = null,
         DateTime? publishedFrom = null,
         DateTime? publishedTo = null,
         int? status = null,
         bool? isFeatured = null,
         bool? isPromoted = null,
         int pageNumber = 1,
         int pageSize = 12,
         CancellationToken cancellationToken = default);

        Task<PaginatedResult<ServiceAdPaymentSummaryDto>> GetAllServiceAdPaymentSummaries(
     int? pageNumber = 1,
     int? pageSize = 12,
     string? search = null,
     string? sortBy = null, 
     CancellationToken cancellationToken = default);

        Task<PaginatedResult<ServiceP2PAdSummaryDto>> GetAllP2PServiceBoAds(
       string? sortBy = "CreationDate",
       string? search = null,
       DateTime? fromDate = null,
       DateTime? toDate = null,
       int pageNumber = 1,
       int pageSize = 12,
       CancellationToken cancellationToken = default);

        Task<PaginatedResult<ServiceSubscriptionAdSummaryDto>> GetAllSubscriptionAdsServiceBo(
                string? sortBy = "CreationDate",
                string? search = null,
                DateTime? fromDate = null,
                DateTime? toDate = null,
                DateTime? publishedFrom = null,
                DateTime? publishedTo = null,
                int pageNumber = 1,
                int pageSize = 12,
                CancellationToken cancellationToken = default);

        Task<List<CompanyProfileDto>> GetCompaniesByVerticalAsync(
    VerticalType verticalId,
    SubVertical? subVerticalId,
    CancellationToken cancellationToken = default);
    }
}
