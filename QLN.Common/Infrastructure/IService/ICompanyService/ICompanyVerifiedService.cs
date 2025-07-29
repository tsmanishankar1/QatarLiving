using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyVerifiedService
    {
        Task<string> CreateCompany(VerifiedCompanyDto dto, CancellationToken cancellationToken = default);
        Task<VerifiedCompanyDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<List<VerifiedCompanyDto>> GetAllCompanies(CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(VerifiedCompanyDto dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(Guid id, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(string userId, CompanyVerificationApproveDto dto, CancellationToken cancellationToken = default);
        Task<CompanyVerifyApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default);
        Task<List<CompanyVerificationStatusDto>> VerificationStatus(Guid userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default);
        Task<List<VerifiedCompanyDto>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<VerificationProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<VerificationProfileStatus>> GetAllVerificationProfiles(VerticalType vertical, SubVertical? subVertical = null, CancellationToken cancellationToken = default);
    }
}
