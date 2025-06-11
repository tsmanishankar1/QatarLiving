using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IEventService
    {
        Task<string> CreateEvent(ContentEventDto dto, CancellationToken cancellationToken = default);
        Task<ContentEventDto?> GetEventById(Guid id, CancellationToken cancellationToken = default);
        Task<List<ContentEventDto>> GetAllEvents(CancellationToken cancellationToken = default);
        Task<string> UpdateEvent(ContentEventDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteEvent(Guid id, CancellationToken cancellationToken = default);
    }
}
