using Dapr.Client;
using Microsoft.AspNetCore.Http;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
                var url = "/api/companyprofile/createByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.CompanyServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error creating company profile");
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
        public async Task<string> UpdateCompany(CompanyProfileDto dto, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/updateByUserId?id={id}";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.CompanyServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
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
        public async Task<List<CompanyProfileCompletionStatusDto>> GetCompanyProfileCompletionStatus(
            Guid userId, VerticalType vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/completionstatusbyuserId?userId={userId}&vertical={vertical}";
                var response = await _dapr.InvokeMethodAsync<List<CompanyProfileCompletionStatusDto>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken);
                return response ?? new();
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID not found.");
                return new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile completion status for user {UserId} and vertical {Vertical}", userId, vertical);
                throw;
            }
        }
        public async Task<List<CompanyProfileVerificationStatusDto>> GetVerificationStatus(Guid userId, VerticalType verticalType, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/verificationstatusbyuserId?userId={userId}&vertical={verticalType}";
                var response = await _dapr.InvokeMethodAsync<List<CompanyProfileVerificationStatusDto>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken);
                return response ?? new List<CompanyProfileVerificationStatusDto>();
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID not found.");
                return new List<CompanyProfileVerificationStatusDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verification status for user {UserId} and vertical {VerticalType}", userId, verticalType);
                throw;
            }
        }
        public async Task<string> ApproveCompany(Guid userId, CompanyApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/approveByUserId?userId={userId}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.CompanyServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode(); 

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
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
                    ConstantValues.CompanyServiceAppId,
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
        public async Task<List<CompanyProfileVerificationStatusDto>> VerificationStatus(Guid userId, bool isVerified, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/verifiedstatusbyuserId?isverified={isVerified.ToString().ToLower()}&userId={userId}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.CompanyServiceAppId, url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("No companies found for user {UserId} with status {IsVerified}", userId, isVerified);
                        return new List<CompanyProfileVerificationStatusDto>();
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<CompanyProfileVerificationStatusDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CompanyProfileVerificationStatusDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Dapr call to get verification status for user {UserId}", userId);
                throw;
            }
        }
    }
}