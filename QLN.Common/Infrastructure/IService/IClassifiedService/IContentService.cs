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
        Task<ContentLandingPageResponse?> GetLandingPageAsync();

        Task<ContentPost?> GetPostBySlugAsync(string slug);
        Task<ContentEvent?> GetEventBySlugAsync(string slug);
    }
}