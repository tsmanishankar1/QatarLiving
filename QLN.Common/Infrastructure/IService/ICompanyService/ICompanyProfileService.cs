using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyProfileService
    {
        Task<string> CreateCompany(string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default);
        Task<CompanyProfileModel?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileModel>> GetAllCompanies(CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(CompanyProfileModel dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(Guid id, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(string userId, CompanyProfileApproveDto dto, CancellationToken cancellationToken = default);
        Task<CompanyProfileApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileVerificationStatus>> VerificationStatus(string userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileModel>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<VerificationCompanyProfileStatus>> GetAllVerificationProfiles(VerticalType vertical, SubVertical? subVertical = null, CancellationToken cancellationToken = default);
    }
}
