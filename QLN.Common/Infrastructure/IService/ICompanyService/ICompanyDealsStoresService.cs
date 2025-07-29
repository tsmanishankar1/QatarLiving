using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyDealsStoresService
    {
        Task<string> CreateCompany(DealsStoresCompanyDto dto, CancellationToken cancellationToken = default);
        Task<DealsStoresCompanyDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<List<DealsStoresCompanyDto>> GetAllCompanies(CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(DealsStoresCompanyDto dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(Guid id, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(Guid userId, CompanyDsApproveDto dto, CancellationToken cancellationToken = default);
        Task<CompanyDsApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default);
        Task<List<CompanyDsVerificationStatusDto>> VerificationStatus(string userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default);
        Task<List<DealsStoresCompanyDto>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default);
        Task<List<DsProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default);
    }
}
