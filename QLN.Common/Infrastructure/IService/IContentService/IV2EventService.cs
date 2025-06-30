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
        Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken = default);
    }
}
