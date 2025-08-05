using QLN.Common.DTO_s;
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
        Task<AdCreatedResponseDto> CreateClassifiedItemsAd(ClassifiedsItems dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> RefreshClassifiedItemsAd(SubVertical subVertical, Guid adId, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(ClassifiedsPreloved dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(ClassifiedsCollectibles dto, CancellationToken cancellationToken = default);
        Task<AdCreatedResponseDto> CreateClassifiedDealsAd(ClassifiedsDeals dto, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedItemsAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedPrelovedAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedCollectiblesAd(Guid adId, CancellationToken cancellationToken = default);
        Task<DeleteAdResponseDto> DeleteClassifiedDealsAd(Guid adId, CancellationToken cancellationToken = default);
        Task<ClassifiedsItems> GetItemAdById(Guid adId, CancellationToken cancellationToken = default);
        Task<ClassifiedsPreloved> GetPrelovedAdById(Guid adId, CancellationToken cancellationToken = default);
        Task<ClassifiedsDeals> GetDealsAdById(Guid adId, CancellationToken cancellationToken = default);
        Task<ClassifiedsCollectibles> GetCollectiblesAdById(Guid adId, CancellationToken cancellationToken = default);
        Task<Guid> CreateCategory(CategoryDtos dto, CancellationToken cancellationToken);
        Task<List<Categories>> GetChildCategories(string vertical, Guid parentId, CancellationToken cancellationToken);
        Task<CategoryTreeDto?> GetCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken);
        Task DeleteCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken);
        Task<List<CategoryTreeDto>> GetAllCategoryTrees(string vertical, CancellationToken cancellationToken);
        Task<List<CategoryField>> GetFiltersByMainCategoryAsync(string vertical, Guid mainCategoryId, CancellationToken cancellationToken);
        Task<AdUpdatedResponseDto> UpdateClassifiedItemsAd(ClassifiedsItems dto, CancellationToken cancellationToken = default);
        Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(ClassifiedsPreloved dto, CancellationToken cancellationToken = default);
        Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(ClassifiedsCollectibles dto, CancellationToken cancellationToken = default);
        Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(ClassifiedsDeals dto, CancellationToken cancellationToken = default);
        Task<PaginatedAdResponseDto> GetFilteredAds(string subVertical,bool? isPublished,int page,int pageSize,string? search,string userId,CancellationToken token);
        Task<BulkAdActionResponse> BulkUpdateAdPublishStatusAsync(string subVertical,string userId,List<Guid> adIds,bool isPublished,CancellationToken cancellationToken = default);    }
}