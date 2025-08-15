using Dapr.Client;
using Microsoft.Extensions.Hosting;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;

namespace QLN.DataMigration.Services
{
    public class ClassifiedsService : IClassifiedService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ClassifiedsService> _logger;

        public ClassifiedsService(
            DaprClient dapr,
            ILogger<ClassifiedsService> logger
            )
        {
            _dapr = dapr;
            _logger = logger;
        }
        public Task<BulkAdActionResponse> BulkUpdateAdPublishStatusAsync(int subVertical, string userId, List<long> adIds, bool isPublished, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> CreateCategory(CategoryDtos dto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<string> MigrateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.PublishEventAsync(
                            pubsubName: ConstantValues.PubSubName,
                            topicName: ConstantValues.PubSubTopics.CollectablesMigration,
                            data: dto,
                            cancellationToken: cancellationToken
                        );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing article {dto.Title} to {ConstantValues.PubSubTopics.CollectablesMigration} topic");
                throw;
            }

            return $"Published article {dto.Title} to {ConstantValues.PubSubTopics.CollectablesMigration} topic";
        }

        public Task<AdCreatedResponseDto> CreateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AdCreatedResponseDto> CreateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AdCreatedResponseDto> CreateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteAdResponseDto> DeleteClassifiedAd(SubVertical subVertical, long adId, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> FeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId,Guid subscriptionid, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<CategoryTreeDto>> GetAllCategoryTrees(string vertical, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<Collectibles>> GetAllCollectiblesAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Deals>> GetAllDealsAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Items>> GetAllItemsAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Preloveds>> GetAllPrelovedAdByUser(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CategoryTreeDto?> GetCategoryTree(string vertical, Guid categoryId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<Categories>> GetChildCategories(string vertical, Guid parentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Collectibles> GetCollectiblesAdById(long adId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Deals> GetDealsAdById(long adId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedAdResponseDto> GetFilteredAds(SubVertical subVertical, bool? isPublished, int page, int pageSize, string? search, string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<CategoryField>> GetFiltersByMainCategoryAsync(string vertical, Guid mainCategoryId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Items> GetItemAdById(long adId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Preloveds> GetPrelovedAdById(long adId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<SavedSearchResponseDto>> GetSearches(string userId, Vertical vertical, SubVertical? subVertical = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
               
        public Task<bool> SaveSearch(SaveSearchRequestDto dto, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveSearchById(SaveSearchRequestByIdDto dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AdUpdatedResponseDto> UpdateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AdUpdatedResponseDto> UpdateClassifiedDealsAd(Deals dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AdUpdatedResponseDto> UpdateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AdUpdatedResponseDto> UpdateClassifiedPrelovedAd(Preloveds dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> MigrateClassifiedItemsAd(Items dto, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.PublishEventAsync(
                            pubsubName: ConstantValues.PubSubName,
                            topicName: ConstantValues.PubSubTopics.ItemsMigration,
                            data: dto,
                            cancellationToken: cancellationToken
                        );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing article {dto.Title} to {ConstantValues.PubSubTopics.ItemsMigration} topic");
                throw;
            }

            return $"Published article {dto.Title} to {ConstantValues.PubSubTopics.ItemsMigration} topic";
        }

        public Task<AdCreatedResponseDto> CreateClassifiedCollectiblesAd(Collectibles dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AdCreatedResponseDto> RefreshClassifiedItemsAd(SubVertical subVertical, long adId, string userId, Guid subscriptionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> Favourite(WishlistCreateDto dto, string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<Wishlist>> GetAllByUserFavouriteList(string userId, Vertical vertical, SubVertical subVertical, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> UnFavourite(string userId, Vertical vertical, SubVertical subVertical, long adId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> FeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> PromoteClassifiedAd(ClassifiedsPromoteDto dto, string userId, Guid subscriptionid, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Items> GetItemAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Preloveds> GetPrelovedAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Collectibles> GetCollectiblesAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Deals> GetDealsAdBySlug(string slug, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SaveSearchByVertical(SaveSearchRequestDto dto, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> UnPromoteClassifiedAd(ClassifiedsPromoteDto dto, string userId, Guid subscriptionid, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> UnFeatureClassifiedAd(ClassifiedsPromoteDto dto, string userId, Guid subscriptionid, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
