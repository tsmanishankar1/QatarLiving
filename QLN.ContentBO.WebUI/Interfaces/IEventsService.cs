using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IEventsService
    {
        Task<HttpResponseMessage> CreateEvent(EventDTO events);
        Task<HttpResponseMessage> GetEventCategories();
        Task<HttpResponseMessage> GetEventLocations();
        Task<HttpResponseMessage> GetAllEvents();
        Task<HttpResponseMessage> DeleteEvent(string eventId);
        Task<HttpResponseMessage> GetFeaturedEvents();
        Task<HttpResponseMessage> UpdateFeaturedEvents(EventDTO events);
    }
}
