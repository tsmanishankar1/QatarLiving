using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyProfileService
    {
        Task<string> CreateCompany(string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default);
        Task<Company?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<Company?> GetCompanyBySlug(string? slug, CancellationToken cancellationToken = default);
        Task<CompanyPaginatedResponse<Company>> GetAllVerifiedCompanies(CompanyProfileFilterRequest filter, CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(Company dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(DeleteCompanyRequest request, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(string userId, CompanyProfileApproveDto dto, CancellationToken cancellationToken = default);
        Task<List<Company>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<CompanySubscriptionListResponseDto> GetCompanySubscriptions(CompanySubscriptionFilter filter, CancellationToken cancellationToken = default);
        Task<string> MigrateCompany(string guid, string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default);
    }
}
