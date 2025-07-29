using QLN.Common.DTO_s;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2EventService
    {
        Task<string> CreateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default);
        Task<V2Events?> GetEventById(Guid id, CancellationToken cancellationToken = default);
        Task<List<V2Events>> GetAllEvents(CancellationToken cancellationToken = default);
        Task<List<V2Events>> GetAllIsFeaturedEvents(bool isFeatured, CancellationToken cancellationToken = default);
        Task<string> UpdateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default);
        Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default);
        Task<string> CreateCategory(EventsCategory dto, CancellationToken cancellationToken = default);
        Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken = default);
        Task<EventsCategory?> GetEventCategoryById(int id, CancellationToken cancellationToken = default);
        Task<PagedResponse<V2Events>> GetPagedEvents(GetPagedEventsRequest request, CancellationToken cancellationToken = default);
        Task<List<V2Slot>> GetAllEventSlot(CancellationToken cancellationToken = default);
        Task<IEnumerable<V2Events>> GetExpiredEvents(CancellationToken cancellationToken = default);
        Task<string> ReorderEventSlotsAsync(EventSlotReorderRequest dto, CancellationToken cancellationToken = default);
        Task<List<V2Events>> GetEventsByStatus(EventStatus status, CancellationToken cancellationToken);
        Task<List<V2Events>> GetEventStatus(EventStatus status, CancellationToken cancellationToken);
        Task UpdateFeaturedEvent(UpdateFeaturedEvent dto, CancellationToken cancellationToken = default);
        Task<string> UnfeatureEvent(Guid id, CancellationToken cancellationToken = default);
    }
}
