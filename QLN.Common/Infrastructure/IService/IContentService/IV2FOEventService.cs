using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2FOEventService
    {
        Task<V2Events?> GetFOEventById(Guid id, CancellationToken cancellationToken = default);
        Task<List<V2Events>> GetAllFOIsFeaturedEvents(bool isFeatured, CancellationToken cancellationToken = default);
        Task<PagedResponse<V2Events>> GetFOPagedEvents(GetPagedEventsRequest request, CancellationToken cancellationToken = default);
        Task<V2Events> GetEventBySlug(string slug, CancellationToken cancellationToken = default);
    }
}
