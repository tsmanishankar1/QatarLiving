using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.MockServices
{
    public class EventsMockService : ServiceBase<EventsMockService>, IEventsService
    {
        public EventsMockService(HttpClient httpClientDI, ILogger<EventsMockService> Logger)
           : base(httpClientDI, Logger)
        {

        }
        public Task<HttpResponseMessage> CreateEvent(EventDTO events)
        {
            throw new NotImplementedException();
        }
        public Task<HttpResponseMessage> GetEventCategories()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(newsCateg), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> GetEventLocations()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(newsCateg), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }

        
    }
}
