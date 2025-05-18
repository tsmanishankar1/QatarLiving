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
        Task<CompanyProfileEntity> CreateAsync(CompanyProfileDto dto, HttpContext context);
        Task<CompanyProfileEntity?> GetAsync(Guid id);
        Task<IEnumerable<CompanyProfileEntity>> GetAllAsync();
        Task<CompanyProfileEntity> UpdateAsync(Guid id, CompanyProfileDto dto, HttpContext context);
        Task DeleteAsync(Guid id);
    }
}
