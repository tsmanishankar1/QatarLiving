using Dapr.Client;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.ComponentModel.Design;
using System.Net;
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
        public const string CompanyServiceAppId = ConstantValues.CompanyServiceAppId;
        public async Task<string> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/companyprofile/create";
                var response = await _dapr.InvokeMethodAsync<CompanyProfileDto, string>(
                    HttpMethod.Post,
                    CompanyServiceAppId,
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
                    CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID {id} not found.", id);
                return null;
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
                    CompanyServiceAppId,
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
                    CompanyServiceAppId,
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
                    CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID {id} not found.", id);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company profile with ID: {Id}", id);
                throw;
            }
        }
        public async Task<CompanyProfileCompletionStatusDto?> GetCompanyProfileCompletionStatus(Guid userId, string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/completion-status?userId={userId}&vertical={vertical}";
                return await _dapr.InvokeMethodAsync<CompanyProfileCompletionStatusDto>(
                    HttpMethod.Get,
                    CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID not found.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile completion status for user {UserId} and vertical {Vertical}", userId, vertical);
                throw;
            }
        }
        public async Task<CompanyProfileVerificationStatusDto?> GetVerificationStatus(Guid userId, VerticalType verticalType, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/verification-status?userId={userId}&vertical={verticalType}";
                return await _dapr.InvokeMethodAsync<CompanyProfileVerificationStatusDto>(
                    HttpMethod.Get,
                    CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID not found.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verification status for user {UserId} and vertical {VerticalType}", userId, verticalType);
                throw;
            }
        }
        public async Task ApproveCompany(CompanyApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/companyprofile/approve";
                await _dapr.InvokeMethodAsync<CompanyApproveDto>(
                    HttpMethod.Post,
                    CompanyServiceAppId,
                    url,
                    dto,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving company profile.");
                throw;
            }
        }
        public async Task<CompanyApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getApproval?companyId={companyId}";
                var response = await _dapr.InvokeMethodAsync<CompanyApprovalResponseDto>(
                    HttpMethod.Get,
                    CompanyServiceAppId,
                    url,
                    cancellationToken);

                return response;
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID {CompanyId} not found.", companyId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approval info for company ID {CompanyId}.", companyId);
                throw;
            }
        }

    }
}