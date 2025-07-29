using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyServiceDependency
    {
        public static RouteGroupBuilder MapCompanyServiceEndpoints(this RouteGroupBuilder group)
        {
            group.MapGetAllCompanyProfiles()
                .MapGetCompanyProfilesByTokenUser()
                .MapGetStatusByTokenUser()
                .MapGetCompanyProfile()
                .MapCreateCompanyProfile()
                .MapUpdateCompanyProfile()
                .MapDeleteCompanyProfile()
                .MapCompanyApproval()
                .MapGetCompanyApprovalInfo()
                .MapVerificationStatus();
            return group;
        }
    }
}
