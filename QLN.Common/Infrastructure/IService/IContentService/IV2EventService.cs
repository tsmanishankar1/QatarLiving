using QLN.Common.DTO_s;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2EventService
    {
        Task<string> CreateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default);
        Task<V2Events?> GetEventById(Guid id, CancellationToken cancellationToken = default);
        Task<List<V2Events>> GetAllEvents(CancellationToken cancellationToken = default);
        Task<string> UpdateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default);
        Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default);
        Task<string> CreateCategory(EventsCategory dto, CancellationToken cancellationToken = default);
        Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken = default);
        Task<EventsCategory?> GetEventCategoryById(int id, CancellationToken cancellationToken = default);
        Task<PagedResponse<V2Events>> GetPagedEvents(int? page, int? perPage, string? search, string? sortOrder,
                    DateOnly? fromDate, DateOnly? toDate, string? filterType, string? location, bool? freeOnly, CancellationToken cancellationToken = default);
        Task<List<V2Slot>> GetAllEventSlot(CancellationToken cancellationToken = default);
        Task<IEnumerable<V2Events>> GetExpiredEvents(CancellationToken cancellationToken = default);
        Task<string> ReorderEventSlotsAsync(EventReorder dto, CancellationToken cancellationToken = default);

    }
}
