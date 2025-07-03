using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IEventsService
    {
        Task<HttpResponseMessage> CreateEvent(EventDTO events);
        Task<HttpResponseMessage> GetEventCategories();
        Task<HttpResponseMessage> GetEventLocations();
    }
}
