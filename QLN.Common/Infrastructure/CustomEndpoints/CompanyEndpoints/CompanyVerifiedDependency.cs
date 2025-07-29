using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyVerifiedDependency
    {
        public static RouteGroupBuilder MapVerifiedCompanyEndpoints(this RouteGroupBuilder group)
        {
            group.MapGetAllVerificationCompanyProfiles()
                .MapGetVerificationCompanyProfilesByTokenUser()
                .MapGetVerificationStatusByTokenUser()
                .MapGetVerificationCompanyProfile()
                .MapCreateVerificationCompanyProfile()
                .MapUpdateVerificationCompanyProfile()
                .MapDeleteVerificationCompanyProfile()
                .MapVerificationCompanyApproval()
                .MapGetVerificationCompanyApprovalInfo()
                .MapVerifyVerificationStatus();
            return group;
        }
    }
}
