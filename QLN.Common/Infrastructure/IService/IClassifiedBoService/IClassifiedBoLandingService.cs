using Microsoft.AspNetCore.Http;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsBoIndex;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
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
        Task<List<SeasonalPicks>> GetSeasonalPickBySlug(string slug, CancellationToken cancellationToken = default);

        Task<List<SeasonalPicks>> GetSeasonalPicks(Vertical vertical, CancellationToken cancellationToken = default);

        Task<List<SeasonalPicks>> GetSlottedSeasonalPicks(Vertical vertical, CancellationToken cancellationToken = default);

        Task<string> ReplaceSlotWithSeasonalPick(string userId, string userName, ReplaceSeasonalPickSlotRequest dto, CancellationToken cancellationToken = default);

        Task<string> ReorderSeasonalPickSlots(string userId, string userName, SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> SoftDeleteSeasonalPick(string pickId, string userId, string userName, Vertical vertical, CancellationToken cancellationToken = default);
        Task<SeasonalPicks> GetSeasonalPickById(string id, CancellationToken cancellationToken = default);
        Task<string> EditSeasonalPick(string userId, string userName, EditSeasonalPickDto dto, CancellationToken cancellationToken = default);

        Task<string> CreateFeaturedStore(string userId, string userName, FeaturedStoreDto dto, CancellationToken cancellationToken = default);

        Task<List<FeaturedStore>> GetFeaturedStores(Vertical vertical, CancellationToken cancellationToken = default);

        Task<List<FeaturedStoreItem>> GetSlottedFeaturedStores(Vertical vertical, CancellationToken cancellationToken = default);
        Task<List<FeaturedStore>> GetFeatureStoreBySlug(string slug, CancellationToken cancellationToken = default);
        Task<string> ReplaceSlotWithFeaturedStore(string userId, string userName, ReplaceFeaturedStoresSlotRequest dto, CancellationToken cancellationToken = default);

        Task<string> ReorderFeaturedStoreSlots(string userId, string userName, FeaturedStoreSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> SoftDeleteFeaturedStore(string storeId, string userId, string userName, Vertical vertical, CancellationToken cancellationToken = default);

        Task<FeaturedStore> GetFeaturedStoreById(string id, CancellationToken cancellationToken = default);

        Task<string> EditFeaturedStore(string userId, string userName, EditFeaturedStoreDto dto, CancellationToken cancellationToken = default);

        Task<string> CreateFeaturedCategory(string userId, string userName, FeaturedCategoryDto dto, CancellationToken cancellationToken = default);

        Task<string> DeleteFeaturedCategory(string id, Vertical vertical, string userId, string userName, CancellationToken cancellationToken = default);

        Task<List<FeaturedCategory>> GetSlottedFeaturedCategory(Vertical vertical, CancellationToken cancellationToken = default);

        Task<FeaturedCategory> GetFeaturedCategoryById(string id, CancellationToken cancellationToken = default);

        Task<List<FeaturedCategory>> GetFeatureCategoryBySlug(string slug, CancellationToken cancellationToken = default);

        Task<string> EditFeaturedCategory(string userId, string userName, EditFeaturedCategoryDto dto, CancellationToken cancellationToken = default);

        Task<List<FeaturedCategory>> GetFeaturedCategoriesByVertical(Vertical vertical, CancellationToken cancellationToken = default);

        Task<string> ReorderFeaturedCategorySlots(string userId, string userName, LandingBoSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> ReplaceFeaturedCategorySlots(string userId, string userName, LandingBoSlotReplaceRequest dto, CancellationToken cancellationToken = default);

        Task<BulkAdActionResponseitems> BulkItemsAction(
BulkActionRequest request,
string UserId, string UserName,
CancellationToken cancellationToken = default);

        Task<BulkAdActionResponseitems> BulkCollectiblesAction(
    BulkActionRequest request,
    string userId,
    CancellationToken cancellationToken = default);
        Task<PaginatedResult<PrelovedAdPaymentSummaryDto>> GetAllPrelovedAdPaymentSummaries(int? pageNumber = 1, int? pageSize = 12, string? search = null,
            string? sortBy = null, CancellationToken cancellationToken = default);

        Task<PaginatedResult<PrelovedAdSummaryDto>> GetAllPrelovedBoAds(string? sortBy = "CreationDate", string? search = null, DateTime? fromDate = null,
            DateTime? toDate = null, DateTime? publishedFrom = null, DateTime? publishedTo = null, int? status = null, bool? isFeatured = null,
            bool? isPromoted = null, int pageNumber = 1, int pageSize = 12, CancellationToken cancellationToken = default);

        Task<PaginatedResult<DealsAdSummaryDto>> GetAllDeals(
                    int? pageNumber = 1,
                    int? pageSize = 12,
                    string? subscriptionType = null,
                    DateOnly? startDate = null,
                    DateOnly? endDate = null,
                    string? search = null,
                    string? sortBy = null,
                    CancellationToken cancellationToken = default);

        Task<PaginatedResult<DealsViewSummaryDto>> DealsViewSummary(
            int? pageNumber = 1,
            int? pageSize = 12,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            string? search = null,
            string? sortBy = null,
            string? status = null,
            bool? isPromoted = null,
            bool? isFeatured = null,
            CancellationToken cancellationToken = default);

        Task<string> SoftDeleteDeals(DealsBulkDelete dto, string userId, CancellationToken cancellationToken = default);

        Task<string> BulkPrelovedAction(BulkActionRequest request, string userId, CancellationToken ct);

        Task<PrelovedTransactionListResponseDto> GetPrelovedTransactionsAsync(
                 int pageNumber,
                 int pageSize,
                 string? searchText,
                 string? dateCreated,
                 string? datePublished,
                 string? dateStart,
                 string? dateEnd,
                 string? status,
                 string sortBy,
                 string sortOrder,
                 CancellationToken cancellationToken = default);
        Task<TransactionListResponseDto> GetTransactionsAsync(TransactionFilterRequestDto request, CancellationToken cancellationToken = default);
        Task<ClassifiedsBoItemsResponseDto> GetAllItems(GetAllSearch request, CancellationToken cancellation = default);
        Task<ClassifiedsBoCollectiblesResponseDto> GetAllCollectibles(GetAllSearch request, CancellationToken cancellation = default);
        Task<string> BulkDealsAction(BulkActionRequest request, string userId, CancellationToken ct);

    }
}

