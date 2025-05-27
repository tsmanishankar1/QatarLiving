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
        Task<QlnContentsDailyPageResponse?> GetContentsDailyPageAsync();

        Task<ContentPost?> GetPostBySlugAsync(string slug);
        Task<ContentEvent?> GetEventBySlugAsync(string slug);
        Task<dynamic?> GetLandingByQueuePageAsync(string queue_name);
        Task<NewsCommunityPageResponse?> GetNewsCommunityAsync();
        Task<NewsQatarPageResponse?> GetNewsQatarAsync();
    }
}