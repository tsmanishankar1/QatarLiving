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
        Task<string> CreateEvent(V2ContentEventDto dto, CancellationToken cancellationToken = default);
        Task<V2ContentEventDto?> GetEventById(Guid id, CancellationToken cancellationToken = default);
        Task<V2ContentEventDto> GetAllEvents(CancellationToken cancellationToken = default);
        Task<string> UpdateEvent(V2ContentEventDto dto, CancellationToken cancellationToken = default);
        Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default);
    }
}
