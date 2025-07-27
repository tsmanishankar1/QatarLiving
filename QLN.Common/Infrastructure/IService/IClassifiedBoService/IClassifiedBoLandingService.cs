using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.V2IClassifiedBoService
{
    public interface IClassifiedBoLandingService
    {       
        Task<string> CreateSeasonalPick(SeasonalPicksDto dto, CancellationToken cancellationToken = default);

        Task<List<SeasonalPicksDto>> GetSeasonalPicks(string vertical, CancellationToken cancellationToken = default);

        Task<List<SeasonalPicksDto>> GetSlottedSeasonalPicks(string vertical, CancellationToken cancellationToken = default);

        Task<string> ReplaceSlotWithSeasonalPick(string vertical, string userId, Guid newPickId, int targetSlot, CancellationToken cancellationToken = default);

        Task<string> ReorderSeasonalPickSlots(SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> SoftDeleteSeasonalPick(string pickId, string userId, string vertical, CancellationToken cancellationToken = default);

        Task<string> CreateFeaturedStore(FeaturedStoreDto dto, CancellationToken cancellationToken = default);
        Task<List<FeaturedStoreDto>> GetFeaturedStores(string vertical, CancellationToken cancellationToken = default);

        Task<List<FeaturedStoreDto>> GetSlottedFeaturedStores(string vertical, CancellationToken cancellationToken = default);

        Task<string> ReplaceSlotWithFeaturedStore(string vertical, string? userId, Guid newStoreId, int targetSlot, CancellationToken cancellationToken = default);

        Task<string> ReorderFeaturedStoreSlots(FeaturedStoreSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> SoftDeleteFeaturedStore(string storeId, string userId, string vertical, CancellationToken cancellationToken = default);

        Task<string> CreateFeaturedCategory(string userId, V2ClassifiedLandingBoDto dto, CancellationToken cancellationToken = default);

        Task<string> DeleteFeaturedCategory(string categoryId, string userId, string vertical, CancellationToken cancellationToken = default);

        Task<List<V2ClassifiedLandingBoDto>> GetSlottedFeaturedCategory(string vertical, CancellationToken cancellationToken = default);

        Task<List<V2ClassifiedLandingBoDto>> GetFeaturedCategoriesByVerticalAsync(string vertical, CancellationToken cancellationToken = default);

        Task<string> ReorderFeaturedCategorySlots(LandingBoSlotReorderRequest request, CancellationToken cancellationToken = default);

        Task<string> ReplaceFeaturedCategorySlots(string userId, LandingBoSlotReplaceRequest dto, CancellationToken cancellationToken = default);
        Task<List<ClassifiedsItems>> BulkAction(BulkActionRequest dto, CancellationToken cancellationToken = default);
    }
}
