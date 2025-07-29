using Dapr.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IEmailService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.CompanyService
{
    public class ExternalCompanyVerifiedService : ICompanyVerifiedService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalCompanyVerifiedService> _logger;
        private readonly IExtendedEmailSender<ApplicationUser> _emailSender;
        private readonly IFileStorageBlobService _blobStorage;
        private readonly UserManager<ApplicationUser> _userManager;
        public ExternalCompanyVerifiedService(DaprClient dapr, ILogger<ExternalCompanyVerifiedService> logger,
            IExtendedEmailSender<ApplicationUser> emailSender, IFileStorageBlobService blobStorage, UserManager<ApplicationUser> userManager)
        {
            _dapr = dapr;
            _logger = logger;
            _emailSender = emailSender;
            _blobStorage = blobStorage;
            _userManager = userManager;
        }
        public async Task<string> CreateCompany(VerifiedCompanyDto dto, CancellationToken cancellationToken = default)
        {
            string? crBlobFileName = null;
            string? logoBlobFileName = null;
            string? cerBlobFileName = null;
            try
            {
                var id = Guid.NewGuid();
                dto.Id = id;
                if (!string.IsNullOrWhiteSpace(dto.CRDocument))
                {
                    var (crExtension, crBase64) = Base64Helper.ParseBase64(dto.CRDocument);
                    if (crExtension is not ("pdf" or "png" or "jpg"))
                        throw new ArgumentException("CR Document must be in PDF, PNG, or JPG format.");

                    crBlobFileName = $"{dto.CompanyName}_{id}.{crExtension}";
                    var crBlobUrl = await _blobStorage.SaveBase64File(crBase64, crBlobFileName, "crdocument", cancellationToken);
                    dto.CRDocument = crBlobUrl;
                }
                if (!string.IsNullOrWhiteSpace(dto.CompanyLogo))
                {
                    string logoExtension;
                    string logoBase64Data;

                    (logoExtension, logoBase64Data) = Base64Helper.ParseBase64(dto.CompanyLogo);

                    if (logoExtension is not ("png" or "jpg"))
                        throw new ArgumentException("Company logo must be in PNG or JPG format.");

                    logoBlobFileName = $"{dto.CompanyName}_{id}.{logoExtension}";
                    var logoBlobUrl = await _blobStorage.SaveBase64File(logoBase64Data, logoBlobFileName, "companylogo", cancellationToken);
                    dto.CompanyLogo = logoBlobUrl;
                }
                var url = "/api/companyverified/createByUserId";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.CompanyServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }

                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, cancellationToken);
                    throw new ConflictException(problem?.Detail ?? "Conflict error.");
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }

            catch (Exception ex)
            {
                await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, cancellationToken);
                _logger.LogError(ex, "Error creating company profile");
                throw;
            }
        }

        private async Task CleanupUploadedFiles(string? crFile, string? logoFile, string? cerFile, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(crFile))
                await _blobStorage.DeleteFile(crFile, "crdocument", cancellationToken);

            if (!string.IsNullOrWhiteSpace(logoFile))
                await _blobStorage.DeleteFile(logoFile, "companylogo", cancellationToken);

            if (!string.IsNullOrWhiteSpace(cerFile))
                await _blobStorage.DeleteFile(cerFile, "therapeuticcertificate", cancellationToken);
        }
        public async Task<VerifiedCompanyDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyverified/getById?id={id}";
                return await _dapr.InvokeMethodAsync<VerifiedCompanyDto>(
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
        public async Task<List<VerifiedCompanyDto>> GetAllCompanies(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<VerifiedCompanyDto>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    "api/companyverified/getAll",
                    cancellationToken);
                return response ?? new List<VerifiedCompanyDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all company profiles.");
                throw;
            }
        }
        public async Task<string> UpdateCompany(VerifiedCompanyDto dto, CancellationToken cancellationToken = default)
        {
            string? crBlobFileName = null;
            string? logoBlobFileName = null;
            string? cerBlobFileName = null;
            try
            {
                var id = dto.Id != Guid.Empty ? dto.Id : Guid.NewGuid();
                dto.Id = id;

                if (!string.IsNullOrWhiteSpace(dto.CRDocument) && !dto.CRDocument.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (crExtension, crBase64) = Base64Helper.ParseBase64(dto.CRDocument);
                    if (crExtension is not ("pdf" or "png" or "jpg"))
                        throw new ArgumentException("CR Document must be in PDF, PNG, or JPG format.");

                    crBlobFileName = $"{dto.CompanyName}_{id}.{crExtension}";
                    var crBlobUrl = await _blobStorage.SaveBase64File(crBase64, crBlobFileName, "crdocument", cancellationToken);
                    dto.CRDocument = crBlobUrl;
                }

                if (!string.IsNullOrWhiteSpace(dto.CompanyLogo) && !dto.CompanyLogo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (logoExtension, logoBase64Data) = Base64Helper.ParseBase64(dto.CompanyLogo);
                    if (logoExtension is not ("png" or "jpg"))
                        throw new ArgumentException("Company logo must be in PNG or JPG format.");

                    logoBlobFileName = $"{dto.CompanyName}_{id}.{logoExtension}";
                    var logoBlobUrl = await _blobStorage.SaveBase64File(logoBase64Data, logoBlobFileName, "companylogo", cancellationToken);
                    dto.CompanyLogo = logoBlobUrl;
                }
                var url = $"/api/companyverified/updateByUserId";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.CompanyServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }

                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, cancellationToken);
                    throw new ConflictException(problem?.Detail ?? "Conflict error.");
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, cancellationToken);
                _logger.LogError(ex, "Error updating company profile");
                throw;
            }
        }
        public async Task DeleteCompany(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyverified/delete?id={id}";
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
        public async Task<string> ApproveCompany(string userId, CompanyVerificationApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                
                var allCompanies = await GetAllCompanies(cancellationToken);
                var company = allCompanies.FirstOrDefault(c => c.Id == dto.CompanyId)
                              ?? throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");
                var url = $"/api/companyverified/approveByUserId?userId={userId}";
                var requestDto = new CompanyVerificationApproveDto
                {
                    CompanyId = dto.CompanyId,
                    IsVerified = dto.IsVerified,
                    Status = dto.Status
                };

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.CompanyServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }

                    throw new InvalidDataException(errorMessage);
                }

                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving company profile.");
                throw;
            }
        }

        public async Task<CompanyVerifyApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyverified/getApproval?companyId={companyId}";
                var response = await _dapr.InvokeMethodAsync<CompanyVerifyApprovalResponseDto>(
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
        public async Task<List<CompanyVerificationStatusDto>> VerificationStatus(Guid userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyverified/verifiedstatusbyuserId" +
                                  $"?isverified={isVerified.ToString().ToLower()}" +
                                  $"&userId={userId}" +
                                  $"&vertical={vertical}";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.CompanyServiceAppId, url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("No companies found for user {UserId} with status {IsVerified}", userId, isVerified);
                        return new List<CompanyVerificationStatusDto>();
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                // TODO: Possibly create this once and reuse it, not creating a new instance every time
                return JsonSerializer.Deserialize<List<CompanyVerificationStatusDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CompanyVerificationStatusDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Dapr call to get verification status for user {UserId}", userId);
                throw;
            }
        }
        public async Task<List<VerifiedCompanyDto>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyverified/getByUserId?userId={userId}";

                return await _dapr.InvokeMethodAsync<List<VerifiedCompanyDto>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No companies found for token user.");
                return new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies for token user");
                throw;
            }
        }
        public async Task<List<VerificationProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyverified/statusByUserId?userId={userId}";

                var companies = await _dapr.InvokeMethodAsync<List<VerificationProfileStatus>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken
                );

                return companies ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status by token user");
                throw;
            }
        }
        public async Task<List<VerificationProfileStatus>> GetAllVerificationProfiles( VerticalType vertical, SubVertical? subVertical = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyverified/profileStatusbyverified?vertical={vertical}";

                if (subVertical.HasValue)
                {
                    url += $"&subVertical={subVertical.Value}";
                }

                var companies = await _dapr.InvokeMethodAsync<List<VerificationProfileStatus>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken
                );

                return companies ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status by vertical and subvertical");
                throw;
            }
        }

    }
}
