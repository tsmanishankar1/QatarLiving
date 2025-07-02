using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Threading;
using static QLN.Common.DTO_s.CommunityBo;
using static QLN.Common.DTO_s.LocationDto;

namespace QLN.Content.MS.Service.CommunityInternalService
{
    public class V2InternalLocationService : V2IContentLocation
    {
        private const string StateStore = "contentstatestore";
        private const string IndexKey = "community-index";
        private readonly ILogger<V2InternalLocationService> _logger;

        private static string GetKey(string id) => $"communitycategory-{id}";

        public Task<LocationZoneListDto> GetAllZonesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var zoneIds = new[]
                {
            "1", "2", "3", "4", "5", "6", "7", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22",
            "23", "24", "25", "26", "27", "28", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40",
            "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56", "57",
            "58", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "71", "74", "75", "78", "79",
            "80", "81", "82", "86", "90", "91", "92"
        };

                var zones = zoneIds
                    .Select(id => new LocationZoneDto { Id = id, Name = $"Zone {id}" })
                    .ToList();

                return Task.FromResult(new LocationZoneListDto { Zones = zones });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching the zones.");

                return Task.FromResult(new LocationZoneListDto { Zones = new List<LocationZoneDto>() });
            }
        }

        public async Task<AddressResponseDto> GetAddressCoordinatesAsync(int? zone, int? street, int? building, string location, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construct the live API URL
                var liveUrl = $"https://test.qatarliving.com/ajax/address-find?zone={zone}&street={street}&building={building}&location={location}";

                // Make the HTTP request to the live API
                var coordinates = await HttpRequestToLiveApi(liveUrl, cancellationToken);

                // Return the coordinates in the AddressResponseDto
                return new AddressResponseDto { Coordinates = coordinates };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting coordinates by details.");
                return null;  
            }
        }

        private static async Task<List<string>> HttpRequestToLiveApi(string liveUrl, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(liveUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // Deserialize the response into a List<string> (latitude and longitude)
                var coordinates = JsonConvert.DeserializeObject<List<string>>(content);
                return coordinates;
            }

            return null; 
        }
    
        public Task<LocationListResponseDto> GetAllCategoriesLocationsAsync(CancellationToken cancellationToken = default)
        {

            return Task.FromResult(new LocationListResponseDto
            {
                Locations = new List<LocationEventDto>()
            });
        }

    }

}