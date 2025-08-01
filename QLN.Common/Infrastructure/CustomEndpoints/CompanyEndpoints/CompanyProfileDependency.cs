using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyProfileDependency
    {
        public static RouteGroupBuilder MapCompanyProfile(this RouteGroupBuilder group)
        {
            group
                .MapCreateProfile()
                .MapGetByCompanyProfile()
                .MapGetAllCompanyProfiles()
                .MapUpdateCompanyProfile()
                .MapDeleteCompanyProfile()
                .MapCompanyApproval()
                .MapGetCompanyApprovalInfo()
                .MapVerificationStatus()
                .MapGetCompanyProfilesByTokenUser()
                .MapGetStatusByTokenUser()
                .MapGetVerificationCompanyStatus();
            return group;
        }
    }
}
