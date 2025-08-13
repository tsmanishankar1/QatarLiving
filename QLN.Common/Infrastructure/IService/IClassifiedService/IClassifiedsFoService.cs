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
        //Task<List<StoresDashboardManagementDto>> GetStoresDashboardManagement(string? userId, CancellationToken cancellationToken = default);
        Task<List<StoresDashboardHeaderDto>> GetStoresDashboardHeader(string? UserId, string? CompanyId,CancellationToken cancellationToken = default);
        Task<List<StoresDashboardSummaryDto>> GetStoresDashboardSummary(string? CompanyId,string? SubscriptionId,CancellationToken cancellationToken = default);
    }
}
