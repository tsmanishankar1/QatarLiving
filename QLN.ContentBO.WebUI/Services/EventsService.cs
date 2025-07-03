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


        public async Task<HttpResponseMessage> CreateEvent(EventDTO events)
        {
            try
    {
        var jsonPayload = JsonSerializer.Serialize(events, new JsonSerializerOptions
        {
            WriteIndented = true // makes it more readable in logs
        });

        Console.WriteLine("Request Payload:");
        Console.WriteLine(jsonPayload);

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
    }
}
