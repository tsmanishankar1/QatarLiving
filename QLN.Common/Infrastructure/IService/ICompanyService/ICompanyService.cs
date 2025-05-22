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
        Task<CompanyProfileEntity> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default);
        Task<CompanyProfileEntity?> GetCompanyById(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<CompanyProfileEntity>> GetAllCompanies();
        Task<CompanyProfileEntity> UpdateCompany(Guid id, CompanyProfileDto dto, CancellationToken cancellationToken = default);
        Task DeleteCompany(Guid id, CancellationToken cancellationToken = default);
    }
}