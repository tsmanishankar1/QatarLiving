using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyEndpointDependency
    {
        public static RouteGroupBuilder MapCompanyEndpoints(this RouteGroupBuilder group)
        {
            group.MapGetAllClassifiedsCompanyProfiles()
                .MapGetClassifiedsCompanyProfilesByTokenUser()
                .MapGetClassifiedsStatusByTokenUser()
                .MapClassifiedsGetCompanyProfile()
                .MapCreateClassifiedsCompanyProfile()
                .MapUpdateClassifiedsCompanyProfile()
                .MapDeleteClassifiedsCompanyProfile()
                .MapCompanyClassifiedsApproval()
                .MapGetCompanyClassifiedsApprovalInfo()
                .MapClassifiedsVerificationStatus();
            return group;
        }
    }
}
