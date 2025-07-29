using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;


namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyService
    {
        Task<string> CreateCompany(ServiceCompanyDto dto, CancellationToken cancellationToken = default);
        Task<ServiceCompanyDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<List<ServiceCompanyDto>> GetAllCompanies(CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(ServiceCompanyDto dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(Guid id, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(Guid userId, CompanyServiceApproveDto dto, CancellationToken cancellationToken = default);
        Task<CompanyServiceApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default);
        Task<List<CompanyServiceVerificationStatusDto>> VerificationStatus(Guid userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default);
        Task<List<ServiceCompanyDto>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<ServiceProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default);
    }
}