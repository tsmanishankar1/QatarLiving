using Dapr.Client;
using Newtonsoft.Json;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
using static QLN.Common.DTO_s.CommunityBo;
using static QLN.Common.DTO_s.LocationDto;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalCommunityService: V2IContentCommunity
    {
        private readonly DaprClient _daprClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<V2ExternalCommunityService> _logger;
        private const string InternalAppId = "qln-content-ms";

        public V2ExternalCommunityService(
            DaprClient daprClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<V2ExternalCommunityService> logger)
        {
            _daprClient = daprClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        public async Task<ForumCategoryListDto> GetAllForumCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Dapr service invocation to internal MS
                var result = await _daprClient.InvokeMethodAsync<ForumCategoryListDto>(
                    HttpMethod.Get,
                    InternalAppId,
                    "api/v2/community/getAllForumCategories",
                    cancellationToken
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking GetAllForumCategories from {AppId}", InternalAppId);
                return new ForumCategoryListDto
                {
                    ForumCategories = new List<ForumCategoryDto>(),
                    
                };
            }
        }


        public async Task<LocationZoneListDto> GetAllZonesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _daprClient.InvokeMethodAsync<LocationZoneListDto>(
                    HttpMethod.Get,
                    InternalAppId,
                    "api/v2/community/getAllZones",
                    cancellationToken
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while invoking GetAllZones from {AppId}", InternalAppId);
                return new LocationZoneListDto
                {
                    Zones = new List<LocationZoneDto>()
                };
            }
        }

        public async Task<AddressResponseDto> GetAddressCoordinatesAsync(int? zone, int? street, int? building, string location, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryString = $"zone={zone ??0}&street={street??0}&building={building??0}&location={location??""}";
                var liveUrl = $"https://test.qatarliving.com/ajax/address-find?{queryString}";

                _logger.LogInformation($"Requesting live URL: {liveUrl}");

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(liveUrl, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        var coordinates = JsonConvert.DeserializeObject<List<string>>(content);

                        if (coordinates != null && coordinates.Count == 2)
                        {
                            return new AddressResponseDto { Coordinates = coordinates };
                        }
                        else
                        {
                            _logger.LogWarning("No coordinates found in the live API response.");
                            return null;
                        }
                    }
                    else
                    {
                        _logger.LogError("Error fetching data from live API: {StatusCode}", response.StatusCode);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking GetAddressCoordinatesAsync.");
                return null;
            }
        }
   
    
        public async Task <LocationNameDtoList> GetAllLocationName(CancellationToken cancellationToken = default)
        {

            try
            {
                var result = await _daprClient.InvokeMethodAsync<LocationNameDtoList>(
                    HttpMethod.Get,
                    InternalAppId,
                    "api/v2/community/getAllLocationName",
                    cancellationToken
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking getAllLocationName from {AppId}", InternalAppId);
                return null;
            }
        }


    }
}