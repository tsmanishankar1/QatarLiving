using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ISearchService
{
    public interface ISearchService
    {
        Task<CommonResponse> SearchAsync(string vertical, SearchRequest request);
        Task<string> UploadAsync(CommonIndexRequest request);
        Task<T?> GetByIdAsync<T>(string vertical, string key);
    }
}
