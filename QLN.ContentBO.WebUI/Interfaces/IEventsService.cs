using System.IdentityModel.Tokens.Jwt;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IEventsService
    {
        Task<HttpResponseMessage> CreateEvent(EventDTO events);
        Task<HttpResponseMessage> GetEventCategories();
        Task<HttpResponseMessage> GetEventLocations();
        Task<HttpResponseMessage> GetAllEvents();
        Task<HttpResponseMessage> GetEventById(Guid eventId);
        Task<HttpResponseMessage> DeleteEvent(string eventId);
        Task<HttpResponseMessage> GetFeaturedEvents();
        Task<HttpResponseMessage> UpdateFeaturedEvents(object payload);
        Task<HttpResponseMessage> GetEventsByPagination(
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
    int? status = null
);


        Task<HttpResponseMessage> UpdateEvents(EventDTO events);
        Task<HttpResponseMessage> ReorderFeaturedSlots(int fromSlot, int toSlot, string userId);

    }
}
