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
        Task<CompanyProfileEntity> CreateAsync(CompanyProfileDto dto, CancellationToken cancellationToken = default);
        Task<CompanyProfileEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<CompanyProfileEntity>> GetAllAsync();
        Task<CompanyProfileEntity> UpdateAsync(Guid id, CompanyProfileDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
