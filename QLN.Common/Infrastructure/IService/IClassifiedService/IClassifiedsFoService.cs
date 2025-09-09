using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsFo;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService 
{
    public interface IClassifiedsFoService
    {
        Task<List<StoresDashboardHeaderDto>> GetStoresDashboardHeader(string? UserId, string? CompanyId,CancellationToken cancellationToken = default);
        Task<List<StoresDashboardSummaryDto>> GetStoresDashboardSummary(string? CompanyId,string? SubscriptionId,CancellationToken cancellationToken = default);
        Task<string> GetFOProcessStoresCSV(string Url, string CsvPlatform, string? CompanyId, string? SubscriptionId,
           string? UserId, string Domain, CancellationToken cancellationToken = default);
    }
}
