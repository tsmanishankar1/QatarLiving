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
                .MapUpdateCompanyProfile()
                .MapGetAllCompanyProfiles()
                .MapUpdateVerifiedCompanyProfile()
                .MapDeleteCompanyProfile()
                .MapCompanyApproval()
                .MapGetCompanyProfilesByTokenUser()
                .MapGetVerificationCompanyStatus();
            return group;
        }
    }
}
