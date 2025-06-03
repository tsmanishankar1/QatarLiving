using QLN.Web.Shared.Services.Interface;
using System.Net;

namespace QLN.Web.Shared.Services
{
    public class EventService : ServiceBase<EventService>, IEventService
    {
        private readonly HttpClient _httpClient;

        public EventService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetAllEventsAsync(string? category_id = null, string? location_id = null, string? date = null)
        {
            var queryString = "";

            if(string.IsNullOrEmpty(category_id))
            {
                queryString = $"category_id={category_id}";
            }

            if (string.IsNullOrEmpty(location_id))
            {
                queryString = $"location_id={location_id}";
            }

            if (string.IsNullOrEmpty(date))
            {
                queryString = $"date={date}";
            }

            try
            {
                var queryUrl = string.IsNullOrEmpty(queryString) ? "api/content/events" : $"api/content/events?{queryString}";
                var response = await _httpClient.GetAsync(queryUrl);
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
                var response = await _httpClient.GetAsync("api/content/qln_contents_daily/landing");
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
