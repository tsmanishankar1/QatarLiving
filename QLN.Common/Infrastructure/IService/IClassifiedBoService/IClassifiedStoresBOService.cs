using QLN.Common.DTO_s.ClassifiedsBo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IClassifiedBoService
{
    public interface IClassifiedStoresBOService
    {
       
        Task<ClassifiedBOPageResponse<ViewStoresSubscriptionDto>> getStoreSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, CancellationToken cancellationToken = default);

        Task<string> GetProcessStoresCSV(string Url,string CsvPlatform, string? CompanyId, string? SubscriptionId,
           string? UserId, string Domain, CancellationToken cancellationToken = default);
    }
}
