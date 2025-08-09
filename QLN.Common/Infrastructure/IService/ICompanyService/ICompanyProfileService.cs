using QLN.Common.DTO_s.Company;

namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyProfileService
    {
        Task<string> CreateCompany(string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default);
        Task<QLN.Common.Infrastructure.Model.Company?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<CompanyPaginatedResponse<QLN.Common.Infrastructure.Model.Company>> GetAllVerifiedCompanies(CompanyProfileFilterRequest filter, CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(QLN.Common.Infrastructure.Model.Company dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(DeleteCompanyRequest request, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(string userId, CompanyProfileApproveDto dto, CancellationToken cancellationToken = default);
        Task<List<QLN.Common.Infrastructure.Model.Company>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default);
    }
}
