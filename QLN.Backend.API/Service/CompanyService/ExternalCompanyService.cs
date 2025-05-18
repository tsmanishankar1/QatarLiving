using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Windows.Input;

namespace QLN.Backend.API.Service.CompanyService
{
    public class ExternalCompanyService : ICompanyService
    {
        private readonly DaprClient _dapr;
        private const string SERVICE_APP_ID = "qln-company-ms";
        private readonly ILogger<ExternalCompanyService> _logger;

        public ExternalCompanyService(DaprClient dapr, ILogger<ExternalCompanyService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<CompanyProfileEntity> CreateAsync(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<CompanyProfileDto, CompanyProfileEntity>(
                   SERVICE_APP_ID, 
                   "companyprofile/create", 
                   dto, 
                   cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating company profile.");
                throw;
            }
        }

        public async Task<CompanyProfileEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<CompanyProfileEntity>(
                    HttpMethod.Get, 
                    SERVICE_APP_ID, 
                    "companyprofile/getById", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving company profile for ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CompanyProfileEntity>> GetAllAsync()
        {
            try
            {
                return await _dapr.InvokeMethodAsync<IEnumerable<CompanyProfileEntity>>(
                    HttpMethod.Get, 
                    SERVICE_APP_ID, 
                    "companyprofile/getAll");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all company profiles.");
                throw;
            }
        }

        public async Task<CompanyProfileEntity> UpdateAsync(Guid id, CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<CompanyProfileDto, CompanyProfileEntity>(
                    HttpMethod.Put, 
                    SERVICE_APP_ID, 
                    "companyprofile/update", 
                    dto, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating company profile with ID: {Id}", id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete, 
                    SERVICE_APP_ID, 
                    "companyprofile/delete", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting company profile with ID: {Id}", id);
                throw;
            }
        }
    }
}
