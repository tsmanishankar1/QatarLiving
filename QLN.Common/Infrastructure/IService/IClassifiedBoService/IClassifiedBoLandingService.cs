using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.V2IClassifiedBoService
{
    public interface IClassifiedBoLandingService
    {
        Task<string> CreateSeasonalPick(string userId, string userName, SeasonalPicksDto dto, CancellationToken cancellationToken = default);

        Task<List<SeasonalPicks>> GetSeasonalPicks(string vertical, CancellationToken cancellationToken = default);

        Task<List<SeasonalPicks>> GetSlottedSeasonalPicks(string vertical, CancellationToken cancellationToken = default);

        Task<string> ReplaceSlotWithSeasonalPick(string userId, ReplaceSeasonalPickSlotRequest dto, CancellationToken cancellationToken = default);

        Task<string> ReorderSeasonalPickSlots(string userId, SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> SoftDeleteSeasonalPick(string pickId, string userId, string vertical, CancellationToken cancellationToken = default);

        Task<string> CreateFeaturedStore(string userId, string userName, FeaturedStoreDto dto, CancellationToken cancellationToken = default);

        Task<List<FeaturedStore>> GetFeaturedStores(string vertical, CancellationToken cancellationToken = default);

        Task<List<FeaturedStore>> GetSlottedFeaturedStores(string vertical, CancellationToken cancellationToken = default);

        Task<string> ReplaceSlotWithFeaturedStore(string userId, ReplaceFeaturedStoresSlotRequest dto, CancellationToken cancellationToken = default);

        Task<string> ReorderFeaturedStoreSlots(string userId, FeaturedStoreSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> SoftDeleteFeaturedStore(string storeId, string userId, string vertical, CancellationToken cancellationToken = default);

        Task<string> CreateFeaturedCategory(string userId, string userName, FeaturedCategoryDto dto, CancellationToken cancellationToken = default);

        Task<string> DeleteFeaturedCategory(string categoryId, string userId, string vertical, CancellationToken cancellationToken = default);

        Task<List<FeaturedCategory>> GetSlottedFeaturedCategory(string vertical, CancellationToken cancellationToken = default);

        Task<List<FeaturedCategory>> GetFeaturedCategoriesByVertical(string vertical, CancellationToken cancellationToken = default);

        Task<string> ReorderFeaturedCategorySlots(string userId, LandingBoSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> ReplaceFeaturedCategorySlots(string userId, LandingBoSlotReplaceRequest dto, CancellationToken cancellationToken = default);

        Task<List<ClassifiedsItems>> BulkAction(BulkActionRequest request, CancellationToken ct);
        Task<PaginatedResult<PrelovedAdPaymentSummaryDto>> GetAllPrelovedAdPaymentSummaries(int? pageNumber = 1, int? pageSize = 12, string? search = null,
            string? sortBy = null, CancellationToken cancellationToken = default);

        Task<PaginatedResult<PrelovedAdSummaryDto>> GetAllPrelovedBoAds(string? sortBy = "CreationDate", string? search = null, DateTime? fromDate = null,
            DateTime? toDate = null, DateTime? publishedFrom = null, DateTime? publishedTo = null, int? status = null, bool? isFeatured = null,
            bool? isPromoted = null, int pageNumber = 1, int pageSize = 12, CancellationToken cancellationToken = default);
    }
}
