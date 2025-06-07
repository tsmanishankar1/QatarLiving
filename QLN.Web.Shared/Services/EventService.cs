using Microsoft.Extensions.Caching.Memory;
using QLN.Common.Infrastructure.Constants;
using QLN.Web.Shared.Services.Interface;
using System.Net;

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
        public async Task<HttpResponseMessage?> GetAllEventsAsync(string? category_id = null, string? location_id = null, string? from = null, string? to = null, int? page = 1, int? page_size = 20, string? order = "asc")
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

    }
}
