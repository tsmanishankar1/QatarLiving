using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService
{
    public interface ISaveSearchService
    {
        Task<bool> SaveSearchAsync(SaveSearchRequestDto dto);
        Task<List<SavedSearchResponseDto>> GetSearchesAsync(string userId);
    }
}
