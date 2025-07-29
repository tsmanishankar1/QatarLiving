using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyDealsStoresDependency
    {
        public static RouteGroupBuilder MapCompanyDealsStoresEndpoints(this RouteGroupBuilder group)
        {
            group.MapGetAllDsCompanyProfiles()
               .MapGetDsCompanyProfilesByTokenUser()
               .MapGetDsStatusByTokenUser()
               .MapGetDsCompanyProfile()
               .MapCreateDsCompanyProfile()
               .MapUpdateDsCompanyProfile()
               .MapDeleteDsCompanyProfile()
               .MapDsCompanyApproval()
               .MapGetDsCompanyApprovalInfo()
               .MapDsVerificationStatus();
            return group;
        }
    }
}
