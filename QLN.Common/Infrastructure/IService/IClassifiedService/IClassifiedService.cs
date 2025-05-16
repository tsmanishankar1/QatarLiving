using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.BannerService
{
    public interface IClassifiedService
    {
        Task<IEnumerable<ClassifiedIndexDto>> Search(ClassifiedSearchRequest request);
        Task<ClassifiedIndexDto?> GetById(string id);
        Task<string> Upload(ClassifiedIndexDto document);
        Task<ClassifiedLandingPageResponse> GetLandingPage();
    }
}