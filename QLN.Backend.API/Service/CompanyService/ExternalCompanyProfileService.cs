using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.CompanyService
{
    public class ExternalCompanyProfileService : ICompanyProfileService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalCompanyProfileService> _logger;
        public ExternalCompanyProfileService(DaprClient dapr, ILogger<ExternalCompanyProfileService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }
        public async Task<string> CreateCompany(string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/createcompanybyuserid?uid={uid}&userName={userName}";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Company.CompanyServiceAppId, url);
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
                    throw new InvalidDataException(errorMessage);
                }
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    throw new ConflictException(problem?.Detail ?? "Conflict error.");
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company profile");
                throw;
            }
        }
        public async Task<CompanyProfileModel?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getcompanybyid?id={id}";
                return await _dapr.InvokeMethodAsync<CompanyProfileModel>(
                    HttpMethod.Get,
                    ConstantValues.Company.CompanyServiceAppId,
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
        public async Task<List<CompanyProfileModel>> GetAllCompanies(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<List<CompanyProfileModel>>(
                    HttpMethod.Get,
                    ConstantValues.Company.CompanyServiceAppId,
                    "api/companyprofile/getallcompanies",
                    cancellationToken);
                return response ?? new List<CompanyProfileModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all company profiles.");
                throw;
            }
        }
        public async Task<CompanyPaginatedResponse<CompanyProfileModel>> GetAllVerifiedCompanies(CompanyProfileFilterRequest filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Company.CompanyServiceAppId, "api/companyprofile/getallcompanies", filter);
                var httpResponse = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var body = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        _logger.LogWarning("400 Bad Request from internal service: {0}", body);

                        try
                        {
                            var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            throw new InvalidDataException(problem?.Detail ?? "Invalid input.");
                        }
                        catch (JsonException)
                        {
                            throw new InvalidDataException("Invalid input: " + body);
                        }
                    }

                    throw new Exception($"Internal service returned {httpResponse.StatusCode}: {body}");
                }

                var content = await httpResponse.Content.ReadFromJsonAsync<CompanyPaginatedResponse<CompanyProfileModel>>();
                return content ?? new CompanyPaginatedResponse<CompanyProfileModel>
                {
                    TotalCount = 0,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Items = new List<CompanyProfileModel>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all verified company profiles.");
                throw;
            }
        }
        public async Task<string> UpdateCompany(CompanyProfileModel dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = dto.Id != Guid.Empty ? dto.Id : Guid.NewGuid();
                dto.Id = id;
                var url = $"/api/companyprofile/updatecompanybyuserid";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.Company.CompanyServiceAppId, url);
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

                    throw new InvalidDataException(errorMessage);
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                    throw new ConflictException(problem?.Detail ?? "Conflict error.");
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company profile");
                throw;
            }
        }
        public async Task DeleteCompany(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/deletecompanyprofile?id={id}";
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    ConstantValues.Company.CompanyServiceAppId,
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
        public async Task<string> ApproveCompany(string userId, CompanyProfileApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var company = allCompanies.FirstOrDefault(c => c.Id == dto.CompanyId) ?? throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");
                var requestDto = new CompanyProfileApproveDto
                {
                    CompanyId = dto.CompanyId,
                    Status = dto.Status
                };

                var url = $"/api/companyprofile/approvecompanybyuserid?userId={userId}";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.Company.CompanyServiceAppId, url);
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
        public async Task<List<CompanyProfileModel>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getbytokenuserid?userId={userId}";

                return await _dapr.InvokeMethodAsync<List<CompanyProfileModel>>(
                    HttpMethod.Get,
                    ConstantValues.Company.CompanyServiceAppId,
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
        public async Task<List<VerificationCompanyProfileStatus>?> GetAllVerificationProfiles(
           VerticalType vertical,
           SubVertical? subVertical,
           CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/profileStatusbyverified?vertical={vertical}";
                if (subVertical.HasValue)
                {
                    url += $"&subVertical={subVertical.Value}";
                }

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.Company.CompanyServiceAppId, url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var companies = await JsonSerializer.DeserializeAsync<List<VerificationCompanyProfileStatus>>(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    _dapr.JsonSerializerOptions,
                    cancellationToken);

                return companies;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status by vertical and subvertical");
                throw;
            }
        }
    }
}
