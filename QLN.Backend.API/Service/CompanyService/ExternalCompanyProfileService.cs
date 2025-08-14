using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
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

        public async Task<string> MigrateCompany(string guid, string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/migratecompanybyuserid?guid={guid}&uid={uid}&userName={userName}";
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
        public async Task<Common.Infrastructure.Model.Company?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getcompanybyid?id={id}";
                return await _dapr.InvokeMethodAsync<QLN.Common.Infrastructure.Model.Company>(
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
        public async Task<Common.Infrastructure.Model.Company?> GetCompanyBySlug(string? slug, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getcompanybyslug?slug={slug}";
                return await _dapr.InvokeMethodAsync<Common.Infrastructure.Model.Company>(
                    HttpMethod.Get,
                    ConstantValues.Company.CompanyServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID {slug} not found.", slug);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company profile", slug);
                throw;
            }
        }

        public async Task<CompanyPaginatedResponse<QLN.Common.Infrastructure.Model.Company>> GetAllVerifiedCompanies(CompanyProfileFilterRequest filter, CancellationToken cancellationToken = default)
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

                var content = await httpResponse.Content.ReadFromJsonAsync<CompanyPaginatedResponse<QLN.Common.Infrastructure.Model.Company>>();
                return content ?? new CompanyPaginatedResponse<QLN.Common.Infrastructure.Model.Company>
                {
                    TotalCount = 0,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Items = new List<QLN.Common.Infrastructure.Model.Company>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all verified company profiles.");
                throw;
            }
        }
        public async Task<string> UpdateCompany(QLN.Common.Infrastructure.Model.Company dto, CancellationToken cancellationToken = default)
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
        public async Task DeleteCompany(DeleteCompanyRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/companyprofile/deletecompanyprofilebyuserid";
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    ConstantValues.Company.CompanyServiceAppId,
                    url,
                    request,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Company with ID {id} not found.", request.Id);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company profile with ID: {Id}", request.Id);
                throw;
            }
        }
        public async Task<string> ApproveCompany(string userId, CompanyProfileApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestDto = new CompanyProfileApproveDto
                {
                    CompanyId = dto.CompanyId,
                    Status = dto.Status,
                    CompanyVerificationStatus = dto.CompanyVerificationStatus
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

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        throw new KeyNotFoundException(problem?.Detail ?? $"Company with ID {dto.CompanyId} not found.");
                    }
                    catch (JsonException)
                    {
                        throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");
                    }
                }

                response.EnsureSuccessStatusCode();
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Company approved successfully";
            }
            catch (KeyNotFoundException)
            {
                throw; 
            }
            catch (InvalidDataException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving company profile for CompanyId: {CompanyId}", dto.CompanyId);
                throw;
            }
        }
        public async Task<List<QLN.Common.Infrastructure.Model.Company>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/companyprofile/getbytokenuserid?userId={userId}";

                return await _dapr.InvokeMethodAsync<List<QLN.Common.Infrastructure.Model.Company>>(
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
        public async Task<CompanySubscriptionListResponseDto> GetCompanySubscriptions(CompanySubscriptionFilter filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/companyprofile/viewstores";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.Company.CompanyServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(filter), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new InvalidDataException(errorJson);
                }

                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Raw JSON from internal API: {RawJson}", rawJson);

                return JsonSerializer.Deserialize<CompanySubscriptionListResponseDto>(
                    rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true } 
                ) ?? new CompanySubscriptionListResponseDto
                {
                    Records = new List<CompanySubscriptionDto>(),
                    TotalRecords = 0,
                    PageNumber = filter.PageNumber ?? 1,
                    PageSize = filter.PageSize ?? 12,
                    TotalPages = 0
                };
            }
            catch(InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company subscriptions from external service");
                throw;
            }
        }

    }
}
