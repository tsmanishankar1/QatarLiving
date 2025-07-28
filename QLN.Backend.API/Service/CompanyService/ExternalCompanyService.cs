using Dapr.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    public class ExternalCompanyService : ICompanyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalCompanyService> _logger;
        private readonly IExtendedEmailSender<ApplicationUser> _emailSender;
        private readonly IFileStorageBlobService _blobStorage;
        private readonly UserManager<ApplicationUser> _userManager;
        public ExternalCompanyService(DaprClient dapr, ILogger<ExternalCompanyService> logger,
            IExtendedEmailSender<ApplicationUser> emailSender, IFileStorageBlobService blobStorage, UserManager<ApplicationUser> userManager)
        {
            _dapr = dapr;
            _logger = logger;
            _emailSender = emailSender;
            _blobStorage = blobStorage;
            _userManager = userManager;
        }
        public async Task<string> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            string? crBlobFileName = null;
            string? logoBlobFileName = null;
            string? cerBlobFileName = null;
            string? coverImageBlobFileName = null;
            string? coverImage1BlobFileName = null;

            try
            {
                var id = Guid.NewGuid();
                dto.Id = id;

                // Sanitize business name to use in blob file names
                var sanitizedBusinessName = SanitizeForBlobName(dto.BusinessName);

                if (!string.IsNullOrWhiteSpace(dto.CRDocument))
                {
                    var (crExtension, crBase64) = Base64Helper.ParseBase64(dto.CRDocument);
                    if (crExtension is not ("pdf" or "png" or "jpg"))
                        throw new ArgumentException("CR Document must be in PDF, PNG, or JPG format.");

                    crBlobFileName = $"{sanitizedBusinessName}_{id}.{crExtension}";
                    var crBlobUrl = await _blobStorage.SaveBase64File(crBase64, crBlobFileName, "crdocument", cancellationToken);
                    dto.CRDocument = crBlobUrl;
                }

                if (!string.IsNullOrWhiteSpace(dto.CompanyLogo))
                {
                    var (logoExtension, logoBase64Data) = Base64Helper.ParseBase64(dto.CompanyLogo);
                    if (logoExtension is not ("png" or "jpg"))
                        throw new ArgumentException("Company logo must be in PNG or JPG format.");

                    logoBlobFileName = $"{sanitizedBusinessName}_{id}.{logoExtension}";
                    var logoBlobUrl = await _blobStorage.SaveBase64File(logoBase64Data, logoBlobFileName, "companylogo", cancellationToken);
                    dto.CompanyLogo = logoBlobUrl;
                }

                if (!string.IsNullOrWhiteSpace(dto.Coverimage1))
                {
                    var (coverImageExtension, coverImageBase64Data) = Base64Helper.ParseBase64(dto.Coverimage1);
                    if (coverImageExtension is not ("png" or "jpg" or "pdf"))
                        throw new ArgumentException("Coverimage1 must be in PNG, JPG, or PDF format.");

                    //coverImageBlobFileName = $"{sanitizedBusinessName}_{id}.{coverImageExtension}";
                     coverImageBlobFileName = $"featured_{Guid.NewGuid():N}".Substring(0, 20) + $".{coverImageExtension}";
                    var coverImageUrl = await _blobStorage.SaveBase64File(coverImageBase64Data, coverImageBlobFileName, "companylogo", cancellationToken);
                    dto.Coverimage1 = coverImageUrl;
                }

                if (!string.IsNullOrWhiteSpace(dto.Coverimage2))
                {
                    var (coverImage1Extension, coverImage1Base64Data) = Base64Helper.ParseBase64(dto.Coverimage2);
                    if (coverImage1Extension is not ("png" or "jpg" or "pdf"))
                        throw new ArgumentException("Coverimage2 must be in PNG, JPG, or PDF format.");

                    //coverImage1BlobFileName = $"{sanitizedBusinessName}_{id}.{coverImage1Extension}";
                    coverImage1BlobFileName = $"featured_{Guid.NewGuid():N}".Substring(0, 20) + $".{coverImage1Extension}";
                    var coverImage1Url = await _blobStorage.SaveBase64File(coverImage1Base64Data, coverImage1BlobFileName, "companylogo", cancellationToken);
                    dto.Coverimage2 = coverImage1Url;
                }

                if (!string.IsNullOrWhiteSpace(dto.TherapeuticCertificate))
                {
                    var (cerExtension, cerBase64Data) = Base64Helper.ParseBase64(dto.TherapeuticCertificate);
                    if (cerExtension is not ("png" or "jpg" or "pdf"))
                        throw new ArgumentException("Certificate must be in PDF, PNG or JPG format.");

                    //cerBlobFileName = $"{sanitizedBusinessName}_{id}.{cerExtension}";
                    cerBlobFileName = $"featured_{Guid.NewGuid():N}".Substring(0, 20) + $".{cerExtension}";
                    var cerBlobUrl = await _blobStorage.SaveBase64File(cerBase64Data, cerBlobFileName, "companylogo", cancellationToken);
                    dto.TherapeuticCertificate = cerBlobUrl;
                }

                var url = "/api/companyprofile/createByUserId";
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

                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, coverImageBlobFileName, coverImage1BlobFileName, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, coverImageBlobFileName, coverImage1BlobFileName, cancellationToken);
                    throw new ConflictException(problem?.Detail ?? "Conflict error.");
                }

                response.EnsureSuccessStatusCode();
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, coverImageBlobFileName, coverImage1BlobFileName, cancellationToken);
                _logger.LogError(ex, "Error creating company profile");
                throw;
            }
        }

        private async Task CleanupUploadedFiles(string? crFile, string? logoFile, string? cerFile,string? coverimage,string? coverimage1, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(crFile))
                await _blobStorage.DeleteFile(crFile, "crdocument", cancellationToken);

            if (!string.IsNullOrWhiteSpace(logoFile))
                await _blobStorage.DeleteFile(logoFile, "companylogo", cancellationToken);

            if (!string.IsNullOrWhiteSpace(cerFile))
                await _blobStorage.DeleteFile(cerFile, "companylogo", cancellationToken);
            if (!string.IsNullOrWhiteSpace(coverimage))
                await _blobStorage.DeleteFile(coverimage, "companylogo", cancellationToken);
            if (!string.IsNullOrWhiteSpace(coverimage1))
                await _blobStorage.DeleteFile(coverimage1, "companylogo", cancellationToken);
        }
        public async Task<CompanyProfileDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getById?id={id}";
                return await _dapr.InvokeMethodAsync<CompanyProfileDto>(
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
        public async Task<List<CompanyProfileDto>> GetAllCompanies(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<CompanyProfileDto>>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    "api/companyprofile/getAll",
                    cancellationToken);
                return response ?? new List<CompanyProfileDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all company profiles.");
                throw;
            }
        }
        public async Task<CompanyProfileDto?> GetAllCompaniesBasedonStatus(string status, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getByStatus?status={status}";
                return await _dapr.InvokeMethodAsync<CompanyProfileDto>(
                    HttpMethod.Get,
                    ConstantValues.CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "No companies found with status {Status}.", status);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company profiles for status: {Status}", status);
                throw;
            }
        }

        public async Task<string> UpdateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            string? crBlobFileName = null;
            string? logoBlobFileName = null;
            string? cerBlobFileName = null;
            string? CoverimageBlobFileName = null;
            string? Coverimage1BlobFileName = null;
            try
            {
                var id = dto.Id != Guid.Empty ? dto.Id : Guid.NewGuid();
                dto.Id = id;

                if (!string.IsNullOrWhiteSpace(dto.CRDocument) && !dto.CRDocument.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (crExtension, crBase64) = Base64Helper.ParseBase64(dto.CRDocument);
                    if (crExtension is not ("pdf" or "png" or "jpg"))
                        throw new ArgumentException("CR Document must be in PDF, PNG, or JPG format.");

                    crBlobFileName = $"{dto.BusinessName}_{id}.{crExtension}";
                    var crBlobUrl = await _blobStorage.SaveBase64File(crBase64, crBlobFileName, "crdocument", cancellationToken);
                    dto.CRDocument = crBlobUrl;
                }

                if (!string.IsNullOrWhiteSpace(dto.CompanyLogo) && !dto.CompanyLogo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (logoExtension, logoBase64Data) = Base64Helper.ParseBase64(dto.CompanyLogo);
                    if (logoExtension is not ("png" or "jpg"))
                        throw new ArgumentException("Company logo must be in PNG or JPG format.");

                    logoBlobFileName = $"{dto.BusinessName}_{id}.{logoExtension}";
                    var logoBlobUrl = await _blobStorage.SaveBase64File(logoBase64Data, logoBlobFileName, "companylogo", cancellationToken);
                    dto.CompanyLogo = logoBlobUrl;
                }
                if (!string.IsNullOrWhiteSpace(dto.Coverimage1) && !dto.Coverimage1.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (CoverimageExtension, CoverimageBase64Data) = Base64Helper.ParseBase64(dto.Coverimage1);
                    if (CoverimageExtension is not ("png" or "jpg"))
                        throw new ArgumentException("Coverimage must be in PNG or JPG format.");

                    CoverimageBlobFileName = $"{dto.BusinessName}_{id}.{CoverimageExtension}";
                    var CoverimageBlobUrl = await _blobStorage.SaveBase64File(CoverimageBase64Data, CoverimageBlobFileName, "CoverimageBlobFileName", cancellationToken);
                    dto.CompanyLogo = CoverimageBlobUrl;
                }
                if (!string.IsNullOrWhiteSpace(dto.Coverimage2) && !dto.Coverimage2.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (Coverimage1Extension, Coverimage1Base64Data) = Base64Helper.ParseBase64(dto.Coverimage1);
                    if (Coverimage1Extension is not ("png" or "jpg"))
                        throw new ArgumentException("Cover image must be in PNG or JPG format.");

                    Coverimage1BlobFileName = $"{dto.BusinessName}_{id}.{Coverimage1Extension}";
                    var Coverimage1BlobUrl = await _blobStorage.SaveBase64File(Coverimage1Base64Data, Coverimage1BlobFileName, "CoverimageBlobFileName", cancellationToken);
                    dto.Coverimage2 = Coverimage1BlobUrl;
                }
                if (!string.IsNullOrWhiteSpace(dto.TherapeuticCertificate) && !dto.TherapeuticCertificate.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    string cerExtension;
                    string cerBase64Data;

                    (cerExtension, cerBase64Data) = Base64Helper.ParseBase64(dto.TherapeuticCertificate);

                    if (cerExtension is not ("png" or "jpg" or "pdf"))
                        throw new ArgumentException("Certificat must be in PDF, PNG or JPG format.");

                    cerBlobFileName = $"{dto.BusinessName}_{id}.{cerExtension}";
                    var cerBlobUrl = await _blobStorage.SaveBase64File(cerBase64Data, cerBlobFileName, "therapeuticcertificate", cancellationToken);
                    dto.TherapeuticCertificate = cerBlobUrl;
                }
                var url = $"/api/companyprofile/updateByUserId";
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

                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName,cerBlobFileName, CoverimageBlobFileName, Coverimage1BlobFileName, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, CoverimageBlobFileName, Coverimage1BlobFileName, cancellationToken);
                    throw new ConflictException(problem?.Detail ?? "Conflict error.");
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(crBlobFileName, logoBlobFileName, cerBlobFileName, CoverimageBlobFileName, Coverimage1BlobFileName, cancellationToken);
                _logger.LogError(ex, "Error updating company profile");
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
        public async Task<List<CompanyProfileCompletionStatusDto?>> GetCompanyProfileCompletionStatus(
            Guid userId, VerticalType vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/completionstatusbyuserId?userId={userId}&vertical={vertical}";

                var response = await _dapr.InvokeMethodAsync<List<CompanyProfileCompletionStatusDto?>>(
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
        public async Task<string> ApproveCompany(Guid userId, CompanyApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var company = allCompanies.FirstOrDefault(c => c.Id == dto.CompanyId) ?? throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");
                var user = await _userManager.FindByIdAsync(company.UserId.ToString()) ?? throw new KeyNotFoundException($"User with ID {company.UserId} not found.");

                if (user.IsCompany == true && company.IsVerified == true)
                    throw new InvalidDataException("Company is already marked as approved.");

                var wasNotVerified = !company.IsVerified.GetValueOrDefault(false);
                var isNowVerified = dto.IsVerified.GetValueOrDefault(false);
                var shouldSendEmail = wasNotVerified && isNowVerified && !string.IsNullOrWhiteSpace(company.Email);

                var requestDto = new CompanyApproveDto
                {
                    CompanyId = dto.CompanyId,
                    IsVerified = dto.IsVerified,
                    Status = dto.Status
                };

                var url = $"/api/companyprofile/approveByUserId?userId={userId}";
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

                if (isNowVerified)
                {
                    user.IsCompany = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    var updateResult = await _userManager.UpdateAsync(user);
                }

                if (shouldSendEmail)
                {
                    var subject = "Company Profile Approved - Qatar Living";
                    var htmlBody = _emailSender.GetApprovalEmailTemplate(company.BusinessName);
                    await _emailSender.SendEmail(company.Email, subject, htmlBody);
                }
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
        public async Task<List<CompanyProfileVerificationStatusDto>> VerificationStatus(Guid userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/verifiedstatusbyuserId" +
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
                        return new List<CompanyProfileVerificationStatusDto>();
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                // TODO: Possibly create this once and reuse it, not creating a new instance every time
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
        public async Task<List<CompanyProfileDto>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getByUserId?userId={userId}";

                return await _dapr.InvokeMethodAsync<List<CompanyProfileDto>>(
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
        public async Task<List<ProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/statusByUserId?userId={userId}";

                var companies = await _dapr.InvokeMethodAsync<List<ProfileStatus>>(
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
        private static string SanitizeForBlobName(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }

            return input.Replace(" ", "_").Trim(); // optional: replace spaces too
        }



    }
}