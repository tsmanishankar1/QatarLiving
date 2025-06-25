using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2EventService
    {
        Task<string> CreateEvent(Guid userId, V2EventForm dto, CancellationToken cancellationToken = default);
        Task<V2EventResponse?> GetEventById(Guid id, CancellationToken cancellationToken = default);
        Task<List<V2EventResponse>> GetAllEvents(CancellationToken cancellationToken = default);
        Task<string> UpdateEvent(Guid userId, V2UpdateRequest dto, CancellationToken cancellationToken = default);
        Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default);
    }
}
