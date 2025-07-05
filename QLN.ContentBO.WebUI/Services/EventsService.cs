using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class EventsService : ServiceBase<EventsService>, IEventsService
    {
        private readonly HttpClient _httpClient;

        public EventsService(HttpClient httpClient, ILogger<EventsService> Logger) : base(httpClient, Logger)
        {
            _httpClient = httpClient;
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
            string? location = null,
            bool? freeOnly = null,
            bool? featuredFirst = null,
             int? status = null)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["perPage"] = perPage.ToString(),
                    ["search"] = search,
                    ["categoryId"] = categoryId?.ToString(),
                    ["sortOrder"] = sortOrder,
                    ["fromDate"] = fromDate,
                    ["toDate"] = toDate,
                    ["filterType"] = filterType,
                    ["location"] = location,
                    ["freeOnly"] = freeOnly?.ToString()?.ToLower(),
                    ["featuredFirst"] = featuredFirst?.ToString()?.ToLower(),
                    ["status"] = status?.ToString()
                };

                var queryString = string.Join("&",
                    queryParams
                        .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                        .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}")
                );

                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/v2/event/getpaginatedevents?{queryString}"
                );

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsByPagination");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> CreateEvent(EventDTO events)
        {
            try
    {
        var jsonPayload = JsonSerializer.Serialize(events, new JsonSerializerOptions
        {
            WriteIndented = true 
        });
        var eventsJson = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/event/create")
                {
                    Content = eventsJson
                };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateEvents");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetAllEvents()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/event/getAll");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAllEvents");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetEventById(Guid eventId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/event/getbyid/{eventId}");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventById");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public Task<HttpResponseMessage> GetAllArticles()
        {
            throw new NotImplementedException();
        }

        public async Task<HttpResponseMessage> GetEventCategories()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/event/getAllCategories");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventCategories");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetNewsCategories()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/getCategories");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsCategories");
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
                Logger.LogError(ex, "GetEventLocations");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> DeleteEvent(string eventId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v2/event/delete/{eventId}");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteEvent");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetFeaturedEvents()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/event/getallfeaturedevents?isFeatured=true");
                var response = await _httpClient.SendAsync(request);
                var rawContent = await response.Content.ReadAsStringAsync();
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetFeaturedEvents");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> UpdateFeaturedEvents(EventDTO events)
        {
            try
            {
                var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
                var request = new HttpRequestMessage(HttpMethod.Put, "api/v2/event/update")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateFeaturedEvents");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
public async Task<HttpResponseMessage> UpdateEvents(EventDTO events)
{
    try
    {
        var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
        var request = new HttpRequestMessage(HttpMethod.Put, "api/v2/event/update")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateFeaturedEvents");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public Task<HttpResponseMessage> GetSlots()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetWriterTags()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> UpdateArticle(NewsArticleDTO newsArticle)
        {
            throw new NotImplementedException();
        }
        public async Task<HttpResponseMessage> ReorderFeaturedSlots(int fromSlot, int toSlot, string userId)
{
    try
    {
        var payload = new
        {
            fromSlot = fromSlot,
            toSlot = toSlot,
            userId = userId
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/event/reorderslots")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        return response;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "ReorderFeaturedSlots");
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}

    }
}
