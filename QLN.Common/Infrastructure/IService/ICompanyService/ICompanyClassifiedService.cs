using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyClassifiedService
    {
        Task<string> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default);
        Task<CompanyProfileDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileDto>> GetAllCompanies(CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(Guid id, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(string userId, CompanyApproveDto dto, CancellationToken cancellationToken = default);
        Task<CompanyApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileVerificationStatusDto>> VerificationStatus(string userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileDto>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<ProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileDto>> GetCompaniesByVerticalAndSubVerticalAsync(
            VerticalType vertical,
            SubVertical subVertical,
            bool? isVerified = null,
            CompanyStatus? status = null,
            CancellationToken cancellationToken = default);
    }
}
