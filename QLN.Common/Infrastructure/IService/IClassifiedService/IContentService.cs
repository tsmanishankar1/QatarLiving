using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.BannerService
{
    public interface IContentService
    {

        Task<ContentPost?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<ContentEvent?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<T?> GetPostsFromDrupalAsync<T>(string queue_name, CancellationToken cancellationToken);
        Task<List<ContentEvent>?> GetEventsFromDrupalAsync(CancellationToken cancellationToken);
    }
}