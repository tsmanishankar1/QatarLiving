using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService
{
    public interface IClassifiedService
    {        
        Task<bool> SaveSearch(SaveSearchRequestDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> SaveSearchById(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default);
        Task<List<SavedSearchResponseDto>> GetSearches(string userId, CancellationToken cancellationToken = default);
        Task<ItemAdsAndDashboardResponse> GetUserItemsAdsWithDashboard(Guid userId, CancellationToken cancellationToken = default);
        Task<PrelovedAdsAndDashboardResponse> GetUserPrelovedAdsAndDashboard(Guid userId, CancellationToken cancellationToken = default);        
        Task<AdCreatedResponseDto> CreateClassifiedItemsAd(ClassifiedItems dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(ClassifiedPreloved dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(ClassifiedCollectibles dto, CancellationToken cancellationToken = default);
        Task<CollectiblesResponse> GetCollectibles(string userId, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedDealsAd(ClassifiedDeals dto, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedItemsAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedPrelovedAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedCollectiblesAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedDealsAd(Guid adId, CancellationToken cancellationToken = default);
        Task<PaginatedAdResponseDto> GetUserPublishedItemsAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<PaginatedAdResponseDto> GetUserUnPublishedItemsAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<PaginatedPrelovedAdResponseDto> GetUserPublishedPrelovedAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<PaginatedPrelovedAdResponseDto> GetUserUnPublishedPrelovedAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkUnpublishItemsAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkPublishItemsAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkPublishPrelovedAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkPublishDealsAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkUnpublishDealsAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkUnpublishPrelovedAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkPublishCollectiblesAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<BulkAdActionResponse> BulkUnpublishCollectiblesAds(Guid userId, List<Guid> adIds, CancellationToken cancellationToken = default);
        Task<PaginatedDealsAdResponseDto> GetUserPublishedDealsAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<PaginatedDealsAdResponseDto> GetUserUnPublishedDealsAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<PaginatedCollectiblesAdResponseDto> GetUserPublishedCollectiblesAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<PaginatedCollectiblesAdResponseDto> GetUserUnPublishedCollectiblesAds(Guid userId, int? page, int? pageSize, CancellationToken cancellationToken = default);
        Task<Guid> CreateCategory(CategoryDtos dto, CancellationToken cancellationToken);
        Task<List<Categories>> GetChildCategories(string vertical, Guid parentId, CancellationToken cancellationToken);
        Task<CategoryTreeDto?> GetCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken);
        Task DeleteCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken);
        Task<List<CategoryTreeDto>> GetAllCategoryTrees(string vertical, CancellationToken cancellationToken);
        Task<List<CategoryField>> GetFiltersByMainCategoryAsync(string vertical, Guid mainCategoryId, CancellationToken cancellationToken);
    }
}