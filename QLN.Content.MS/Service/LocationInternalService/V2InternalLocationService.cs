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
                return null;  // Return null in case of error
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

            return null; // Return null if the response is not successful
        }
        public Task<LocationNameDtoList> GetAllLocationName(CancellationToken cancellationToken = default)
        {
            var categories = new List<LocationNameDto>
    {
        new LocationNameDto { Name = "Souq Waqif" },
        new LocationNameDto { Name = "mushaireb" },
        new LocationNameDto { Name = "Mushaireb" },
        new LocationNameDto { Name = "al najada" },
        new LocationNameDto { Name = "Al Najada" },
        new LocationNameDto { Name = "old al ghanim" },
        new LocationNameDto { Name = "Old Al Ghanim" },
        new LocationNameDto { Name = "al corniche" },
        new LocationNameDto { Name = "Al Corniche" },
        new LocationNameDto { Name = "al bidda" },
        new LocationNameDto { Name = "Fereej Abdel Aziz" },
        new LocationNameDto { Name = "fereej abdel aziz" },
        new LocationNameDto { Name = "فريج عبد العزيز" },
        new LocationNameDto { Name = "al doha al jadeeda" },
        new LocationNameDto { Name = "Al Doha Al Jadeeda" },
        new LocationNameDto { Name = "الدوحة الجديدة" },
        new LocationNameDto { Name = "Doha15" },
        new LocationNameDto { Name = "Al Dafna" },
        new LocationNameDto { Name = "الغانم القديم" },
        new LocationNameDto { Name = "Old Al Hitmi" },
        new LocationNameDto { Name = "old al hitmi" },
        new LocationNameDto { Name = "old salata" },
        new LocationNameDto { Name = "Old Salata" },
        new LocationNameDto { Name = "Al Mirqab" },
        new LocationNameDto { Name = "al mirqab" },
        new LocationNameDto { Name = "doha" },
        new LocationNameDto { Name = "doha port" },
        new LocationNameDto { Name = "wadi al sail" },
        new LocationNameDto { Name = "Wadi Al Sail" },
        new LocationNameDto { Name = "Al Rumaila" },
        new LocationNameDto { Name = "fereej bin mahmoud" },
        new LocationNameDto { Name = "Fereej Bin Mahmoud" },
        new LocationNameDto { Name = "فريج بن محمود" },
        new LocationNameDto { Name = "Al Muntazah" },
        new LocationNameDto { Name = "al muntazah" },
        new LocationNameDto { Name = "Rawdat Al Khail" },
        new LocationNameDto { Name = "rawdat al khail" },
        new LocationNameDto { Name = "روضة الخيل" },
        new LocationNameDto { Name = "al mansoura fereej bin dirham" },
        new LocationNameDto { Name = "Al Mansoura / Fereej Bin Dirham" },
        new LocationNameDto { Name = "المنصورة / فريج بن درهم" },
        new LocationNameDto { Name = "Najma" },
        new LocationNameDto { Name = "najma" },
        new LocationNameDto { Name = "النجمة" },
        new LocationNameDto { Name = "Umm Ghwailina" },
        new LocationNameDto { Name = "umm ghwailina" },
        new LocationNameDto { Name = "أم غويلينا" },
        new LocationNameDto { Name = "ras abu abboud" },
        new LocationNameDto { Name = "Ras Abu Abboud" },
        new LocationNameDto { Name = "Al Khulaifat" },
        new LocationNameDto { Name = "al khulaifat" },
        new LocationNameDto { Name = "al duhail" },
        new LocationNameDto { Name = "Al Duhail" },
        new LocationNameDto { Name = "الدحيل" },
        new LocationNameDto { Name = "umm lekhba" },
        new LocationNameDto { Name = "Umm Lekhba" },
        new LocationNameDto { Name = "أم لخبا" },
        new LocationNameDto { Name = "Madinat Khalifa North / Dahl Al Hamam" },
        new LocationNameDto { Name = "madinat khalifa north dahl al hamam" },
        new LocationNameDto { Name = "مدينة خليفة الشمالية / دحل الحمام" },
        new LocationNameDto { Name = "Al Markhiya" },
        new LocationNameDto { Name = "al markhiya" },
        new LocationNameDto { Name = "المرخية" },
        new LocationNameDto { Name = "Madinat Khalifa South" },
        new LocationNameDto { Name = "madinat khalifa south" },
        new LocationNameDto { Name = "مدينة خليفة الجنوبية" },
        new LocationNameDto { Name = "Fereej Kulaib" },
        new LocationNameDto { Name = "fereej kulaib" },
        new LocationNameDto { Name = "فريج كليب" },
        new LocationNameDto { Name = "Al Messila" },
        new LocationNameDto { Name = "al messila" },
        new LocationNameDto { Name = "المسيلة" },
        new LocationNameDto { Name = "Fereej Bin Omran" },
        new LocationNameDto { Name = "fereej bin omran" },
        new LocationNameDto { Name = "فريج بن عمران" }
    };

            return Task.FromResult(new LocationNameDtoList { Locations = categories });
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