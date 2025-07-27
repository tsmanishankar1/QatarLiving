using QLN.Common.DTO_s;


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
    }
}
