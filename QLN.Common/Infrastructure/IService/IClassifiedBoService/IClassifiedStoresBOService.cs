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
        Task<string> CreateStoreSubscriptions(StoresSubscriptionDto dto, CancellationToken cancellationToken = default);
        Task<ClassifiedBOPageResponse<StoresSubscriptionDto>> getStoreSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, CancellationToken cancellationToken = default);
        Task<string> EditStoreSubscriptions(int OrderID, string Status, CancellationToken cancellationToken = default);
        Task<string> GetTestXMLValidation(CancellationToken cancellationToken = default);
        Task<string> GetProcessStoresXML(string Url, string? CompanyId, string? SubscriptionId, string UserName, CancellationToken cancellationToken = default);
    }
}
