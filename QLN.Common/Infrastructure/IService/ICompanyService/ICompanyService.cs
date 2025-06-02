using Microsoft.AspNetCore.Http;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.ICompanyService
{
    public interface ICompanyService
    {
        Task<string> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default);
        Task<CompanyProfileEntity?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileEntity>> GetAllCompanies(CancellationToken cancellationToken = default);
        Task<string> UpdateCompany(CompanyProfileDto dto, Guid id, CancellationToken cancellationToken = default);
        Task DeleteCompany(Guid id, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileCompletionStatusDto?>> GetCompanyProfileCompletionStatus(Guid userId, VerticalType vertical, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileVerificationStatusDto>> GetVerificationStatus(Guid userId, VerticalType verticalType, CancellationToken cancellationToken = default);
        Task<string> ApproveCompany(Guid userId, CompanyApproveDto dto, CancellationToken cancellationToken = default);
        Task<CompanyApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default);
        Task<List<CompanyProfileVerificationStatusDto>> VerificationStatus(Guid userId, bool isVerified, CancellationToken cancellationToken = default);
    }
}