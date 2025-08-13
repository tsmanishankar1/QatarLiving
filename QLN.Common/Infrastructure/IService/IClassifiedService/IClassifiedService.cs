using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.DTO_s.ClassifiedsBo;
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
        Task<bool> SaveSearch(SaveSearchRequestDto dto, string userId, CancellationToken cancellationToken = default);
        Task<bool> SaveSearchById(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default);
        Task<List<SavedSearchResponseDto>> GetSearches(string userId, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default);
        Task<string> MigrateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> RefreshClassifiedItemsAd(
      SubVertical subVertical,
      long adId,
      string userId,
      Guid subscriptionId,
      CancellationToken cancellationToken);
        Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default);
        Task<string> MigrateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default);       
        Task<DeleteAdResponseDto> DeleteClassifiedAd(SubVertical subVertical, long adId, string userId, CancellationToken cancellationToken = default);
        Task<Items> GetItemAdById(long adId, CancellationToken cancellationToken = default);
        Task<List<Items>> GetAllItemsAdByUser(string userId, CancellationToken cancellationToken = default);
        Task<List<Preloveds>> GetAllPrelovedAdByUser(string userId, CancellationToken cancellationToken = default);
        Task<List<Collectibles>> GetAllCollectiblesAdByUser(string userId, CancellationToken cancellationToken = default);
        Task<List<Deals>> GetAllDealsAdByUser(string userId, CancellationToken cancellationToken = default);
        Task<Preloveds> GetPrelovedAdById(long adId, CancellationToken cancellationToken = default);
        Task<Deals> GetDealsAdById(long adId, CancellationToken cancellationToken = default);
        Task<Collectibles> GetCollectiblesAdById(long adId, CancellationToken cancellationToken = default);
        Task<Guid> CreateCategory(CategoryDtos dto, CancellationToken cancellationToken);
        Task<List<Categories>> GetChildCategories(string vertical, Guid parentId, CancellationToken cancellationToken);
        Task<CategoryTreeDto?> GetCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken);
        Task DeleteCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken);
        Task<List<CategoryTreeDto>> GetAllCategoryTrees(string vertical, CancellationToken cancellationToken);
        Task<List<CategoryField>> GetFiltersByMainCategoryAsync(string vertical, Guid mainCategoryId, CancellationToken cancellationToken);
        Task<AdUpdatedResponseDto> UpdateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default);
        Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default);
        Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default);
        Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default);
        Task<PaginatedAdResponseDto> GetFilteredAds(SubVertical subVertical, bool? isPublished, int page, int pageSize, string? search, string userId, CancellationToken cancellationToken);
             
        Task<BulkAdActionResponse> BulkUpdateAdPublishStatusAsync(
     int subVertical,
     string userId,
     List<long> adIds,
     bool isPublished,
     CancellationToken cancellationToken = default);
        Task<string> FeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId,Guid subscriptionid, CancellationToken cancellationToken);
        Task<string> PromoteClassifiedAd(ClassifiedsPromoteDto dto, string userId,Guid subscriptionid, CancellationToken cancellationToken);
    }
}