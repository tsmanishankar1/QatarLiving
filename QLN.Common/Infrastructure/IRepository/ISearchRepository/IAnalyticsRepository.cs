using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IRepository.ISearchServiceRepository
{
    public interface IAnalyticsRepository
    {
        Task<AnalyticsIndex?> GetByKeyAsync(string key);
        Task UpsertAsync(AnalyticsIndex item);
    }
}
