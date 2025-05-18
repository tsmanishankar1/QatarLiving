using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Windows.Input;

namespace QLN.Backend.API.Service.CompanyService
{
    public class ExternalCompanyService : ICompanyService
    {
        private readonly DaprClient _dapr;

        public ExternalCompanyService(DaprClient dapr)
        {
            _dapr = dapr;
        }
        public async Task<CompanyProfileEntity> CreateAsync(CompanyProfileDto dto, HttpContext context)
        {
            return await _dapr.InvokeMethodAsync<CompanyProfileDto, CompanyProfileEntity>(
                "qln-companyprofile-ms", "companyprofile/create", dto);
        }

        public async Task<CompanyProfileEntity?> GetAsync(Guid id)
        {
            return await _dapr.InvokeMethodAsync<CompanyProfileEntity>(
                HttpMethod.Get, "qln-companyprofile-ms", $"companyprofile/getById?id={id}");
        }

        public async Task<IEnumerable<CompanyProfileEntity>> GetAllAsync()
        {
            return await _dapr.InvokeMethodAsync<IEnumerable<CompanyProfileEntity>>(
                HttpMethod.Get, "qln-companyprofile-ms", "companyprofile/getAll");
        }

        public async Task<CompanyProfileEntity> UpdateAsync(Guid id, CompanyProfileDto dto, HttpContext context)
        {
            return await _dapr.InvokeMethodAsync<CompanyProfileDto, CompanyProfileEntity>(
                HttpMethod.Put, "qln-companyprofile-ms", $"companyprofile/update?id={id}", dto);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _dapr.InvokeMethodAsync(HttpMethod.Delete, "qln-companyprofile-ms", $"companyprofile/delete?id={id}");
        }
    }
}
