using Microsoft.Extensions.Caching.Memory;
using QLN.Common.Infrastructure.Constants;
using QLN.Web.Shared.Services.Interface;
using System.Net;
using System.Text.Json;
using System.Text;


namespace QLN.Web.Shared.Services
{
    public class EventService : ServiceBase<EventService>, IEventService
    {
        private readonly HttpClient _httpClient;

        private readonly IMemoryCache _memoryCache;
        private const string BannerCacheKey = "BannerResponseCacheKey";

        public EventService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetAllEventsAsync(string? category_id = null, string? location_id = null, string? from = null, string? to = null, int? page = 1, int? page_size = 20, string? order = "desc")
        {

            string requestUri = $"api/content/events?page={page}&page_size={page_size}&order={order}";
            if (!string.IsNullOrEmpty(category_id))
                requestUri += $"&category_id={category_id}";

            if (!string.IsNullOrEmpty(location_id))
                requestUri += $"&location_id={location_id}";

            if (!string.IsNullOrEmpty(from))
                requestUri += $"&from={from}";

            if (!string.IsNullOrEmpty(to))
                requestUri += $"&to={to}";

            try
            {
                var response = await _httpClient.GetAsync(requestUri);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllEventsAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetEventBySlugAsync(string eventSlug)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/content/event/{eventSlug}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEventBySlugAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetEventCategAndLoc()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/content/categories");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEventCategAndLoc" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetFeaturedEventsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/content/qln_events/landing");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetDailyLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetBannerAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/banner");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetBannerAsync: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAllEventsV2Async(bool? isFeatured = true)
{
    try
    {
        var isFeaturedValue = isFeatured ?? true;

        var url = $"api/v2/event/getallfeaturedevents?isFeatured={isFeaturedValue.ToString().ToLower()}";

        var response = await _httpClient.GetAsync(url);
        return response;
    }
    catch (Exception ex)
    {
        Console.WriteLine("GetAllEventsV2Async: " + ex);
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}

        public async Task<HttpResponseMessage?> GetEventByIdV2Async(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/v2/event/getbyid/{id}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEventByIdV2Async: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetEventLocations()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/location/getAllCategoriesLocations");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetEventCategoriesV2()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/event/getallcategories");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
       public async Task<HttpResponseMessage> GetEventsByPagination(
    int page,
    int perPage,
    string? search = null,
    int? categoryId = null,
    string? sortOrder = null,
    string? fromDate = null,
    string? toDate = null,
    string? filterType = null,
    List<int>? locationId = null,
    bool? freeOnly = null,
    bool? featuredFirst = null,
    int? status = null
)
{
    try
    {
        var requestBody = new
        {
            page = page,
            perPage = perPage,
            status = status ?? 1,
            search = search ?? "",
            categoryId = categoryId ?? 0,
            sortOrder = sortOrder ?? "desc",
            fromDate = fromDate,
            toDate = toDate,
            filterType = filterType ?? "",
            locationId = locationId ?? new List<int>(),
            freeOnly = freeOnly ?? false,
            featuredFirst = featuredFirst ?? false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("/api/v2/event/getpaginatedevents", content);
        return response;
    }
    catch (Exception ex)
    {
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}



    }
}
