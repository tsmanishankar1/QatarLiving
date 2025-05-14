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
        Task<ClassifiedLandingPageResponse> GetLandingPageAsync(string vertical);
        Task<IEnumerable<ClassifiedIndexDto>> SearchAsync(
            string vertical,
            ClassifiedSearchRequest request
        );

        Task<ClassifiedIndexDto?> GetByIdAsync(
            string vertical,
            string id
        );
        Task<string> UploadAsync(
            string vertical,
            ClassifiedIndexDto document
        );
    }
}
