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
        Task<CommonSearchResponse> SearchAsync(string indexName, CommonSearchRequest request);
        Task<string> UploadAsync(CommonIndexRequest request);
        Task<T?> GetByIdAsync<T>(string indexName, string key);
        Task DeleteAsync(string indexName, string key);
        Task<GetWithSimilarResponse<T>> GetByIdWithSimilarAsync<T>(
           string indexName,
           string key,
           int similarPageSize = 10
       ) where T : class;
    }
}
