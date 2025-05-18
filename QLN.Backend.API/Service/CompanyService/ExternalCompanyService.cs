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
        public async Task<CompanyProfileEntity> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<CompanyProfileDto, CompanyProfileEntity>(
                   ConstantValues.CompanyServiceAppId,
                   "/api/companyprofile/create",
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
                var response = await _dapr.InvokeMethodAsync<CompanyProfileEntity>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    "/api/companyprofile/getById", cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving company profile for ID: {Id}", id);
                throw;
            }
        }
        public async Task<IEnumerable<CompanyProfileEntity>> GetAllCompanies()
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<IEnumerable<CompanyProfileEntity>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    "/api/companyprofile/getAll");
                return response;
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
                var response = await _dapr.InvokeMethodAsync<CompanyProfileDto, CompanyProfileEntity>(
                    HttpMethod.Put,
                    ConstantValues.CompanyServiceAppId,
                    "/api/companyprofile/update",
                    dto,
                    cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating company profile with ID: {Id}", id);
                throw;
            }
        }
        public async Task DeleteCompany(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    ConstantValues.CompanyServiceAppId,
                    "/api/companyprofile/delete",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting company profile with ID: {Id}", id);
                throw;
            }
        }
    }
}

