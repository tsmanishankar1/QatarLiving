using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.CommunityBo;
using static QLN.Common.DTO_s.LocationDto;

namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface V2IContentLocation
    {
        //Get All Zones dropdown
        Task<LocationZoneListDto> GetAllZonesAsync(CancellationToken cancellationToken = default);

        Task<AddressResponseDto> GetAddressCoordinatesAsync(int? zone, int? street, int? building, string location, CancellationToken cancellationToken = default);
        Task<LocationNameDtoList> GetAllLocationName(CancellationToken cancellationToken = default);
        Task<LocationListResponseDto> GetAllCategoriesLocationsAsync(CancellationToken cancellationToken = default);


    }
}
