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
        public EventsService(HttpClient httpClientDI, ILogger<EventsService> Logger)
           : base(httpClientDI, Logger)
        {

        }

        public async Task<HttpResponseMessage> CreateEvent(EventDTO events)
        {
            try
            {
                var eventsJson = new StringContent(JsonSerializer.Serialize(events), Encoding.UTF8, "application/json");
                var response = await PostAsync("api/v2/event/create", eventsJson);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateEvents");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetAllArticle(int id)
        {
            try
            {
                var response = await GetAsync($"api/GetAllArticles");
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAllArticle");
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
                var response = await GetAsync($"api/v2/event/getAllCategories");
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsCategories");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetEventLocations()
        { 
             try
            {
                var response = await GetAsync($"api/v2/event/getAllCategories");
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
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
