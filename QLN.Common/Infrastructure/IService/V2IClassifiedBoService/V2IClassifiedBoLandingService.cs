using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.V2IClassifiedBoService
{
    public interface V2IClassifiedBoLandingService
    {
        Task<List<L1CategoryDto>> GetL1CategoriesByVerticalAsync(string vertical, CancellationToken cancellationToken = default);

        Task<string> CreateLandingBoItemAsync(string userId, V2ClassifiedLandingBoDto dto, CancellationToken cancellationToken = default);

        Task<string> CreateSeasonalPick(SeasonalPicksDto dto, CancellationToken cancellationToken = default);

        Task<List<SeasonalPicksDto>> GetSeasonalPicks(CancellationToken cancellationToken = default);

    }
}
