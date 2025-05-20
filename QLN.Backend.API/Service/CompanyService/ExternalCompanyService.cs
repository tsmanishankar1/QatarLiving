using Dapr.Client;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
namespace QLN.Backend.API.Service.CompanyService
{
    public class ExternalCompanyService : ICompanyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalCompanyService> _logger;

        public ExternalCompanyService(DaprClient dapr, ILogger<ExternalCompanyService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }
        public async Task<string> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/companyprofile/create";
                var response = await _dapr.InvokeMethodAsync<CompanyProfileDto, string>(
                    HttpMethod.Post,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    dto,
                    cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating company profile.");
                throw;
            }
        }
        public async Task<CompanyProfileEntity?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getById?id={id}";
                return await _dapr.InvokeMethodAsync<CompanyProfileEntity>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company profile for ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<CompanyProfileEntity>> GetAllCompanies(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<CompanyProfileEntity>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    "api/companyprofile/getAll",
                    cancellationToken);
                return response ?? new List<CompanyProfileEntity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all company profiles.");
                throw;
            }
        }
        public async Task<CompanyProfileEntity> UpdateCompany(Guid id, CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/update?id={id}";
                var response = await _dapr.InvokeMethodAsync<CompanyProfileDto, CompanyProfileEntity>(
                    HttpMethod.Put,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    dto,
                    cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company profile with ID: {Id}", id);
                throw;
            }
        }
        public async Task DeleteCompany(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/delete?id={id}";
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company profile with ID: {Id}", id);
                throw;
            }
        }
    }
}