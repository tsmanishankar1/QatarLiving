using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text;
using System.Text.Json;
using static QLN.Common.DTO_s.V2ReportCommunityPost;

namespace QLN.Backend.API.Service.V2ContentService

{
    public class V2ExternalReportsService : IV2ReportsService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalReportsService> _logger;


        public V2ExternalReportsService(DaprClient dapr, ILogger<V2ExternalReportsService> logger)
        {
            _dapr = dapr;
            _logger = logger;

        }
        public async Task<string> CreateArticleComment(string userName, V2NewsCommunitycommentsDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/createarticlecommentByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
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

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize report response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }
        public async Task<string> CreateCommunityCommentReport(string userName, V2ReportsCommunitycommentsDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/createcommunitycommentsByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
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

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize report response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }
        public async Task<string> CreateReport(string userName, V2ContentReportArticleDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/createByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
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

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize report response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }
        public async Task<string> CreateCommunityReport(string userName, V2ReportCommunityPostDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/createcommunitypostByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
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

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize report response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }
        public async Task<PagedResult<V2ContentReportArticleResponseDto>> GetAllReports( string sortOrder = "desc", int pageNumber = 1, int pageSize = 12, string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryString = $"?sortOrder={sortOrder}&pageNumber={pageNumber}&pageSize={pageSize}";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    queryString += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
                }

                var result = await _dapr.InvokeMethodAsync<PagedResult<V2ContentReportArticleResponseDto>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    $"/api/v2/report/getAll{queryString}",
                    cancellationToken
                );

                return result ?? new PagedResult<V2ContentReportArticleResponseDto>
                {
                    TotalCount = 0,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    Items = new List<V2ContentReportArticleResponseDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all reports.");
                throw;
            }
        }

        public async Task<string> UpdateReportStatus(V2UpdateReportStatusDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/updatearticlecommentstatus";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
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

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize update response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating article comment status");
                throw;
            }
        }
        public async Task<PaginatedCommunityPostResponse> GetAllCommunityPostsWithPagination(int? pageNumber, int? perPage, string? searchTitle = null, string? sortBy = null, CancellationToken ct = default)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"pageNumber={pageNumber ?? 1}",
                    $"perPage={perPage ?? 12}"
                };
                if (!string.IsNullOrWhiteSpace(searchTitle))
                    queryParams.Add($"searchTitle={Uri.EscapeDataString(searchTitle)}");
                if (!string.IsNullOrWhiteSpace(sortBy))
                    queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

                var url = $"/api/v2/report/getallcommunitypostswithpagination?{string.Join("&", queryParams)}";

                var response = await _dapr.InvokeMethodAsync<PaginatedCommunityPostResponse>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    ct);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated community posts with reports.");
                throw;
            }
        }
        public async Task<string> UpdateCommunityPostReportStatus(V2ReportStatus dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/updatecommunitypoststatus";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
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

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning("Received empty response from content service.");
                    return "Empty response from content service";
                }

                try
                {
                    return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
                }
                catch (JsonException jsonEx)
                {
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

     public async Task<PagedResult<V2ContentReportCommunityCommentResponseDto>> GetAllCommunityCommentReports( string sortOrder = "desc",int pageNumber = 1,int pageSize = 12,string? searchTerm = null,CancellationToken cancellationToken = default)
        {
            try
            {
                var query = $"?sortOrder={sortOrder}&pageNumber={pageNumber}&pageSize={pageSize}";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";

                var result = await _dapr.InvokeMethodAsync<PagedResult<V2ContentReportCommunityCommentResponseDto>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    $"/api/v2/report/getAllCommunityCommentReports{query}",
                    cancellationToken
                );

                return result ?? new PagedResult<V2ContentReportCommunityCommentResponseDto>
                {
                    TotalCount = 0,
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    Items = new List<V2ContentReportCommunityCommentResponseDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching community comment reports from internal service");
                throw;
            }
        }

        public async Task<string> UpdateCommunityCommentReportStatus(V2UpdateCommunityCommentReportDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/updatecommunitycommentreportstatus";
                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Put,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation("🔄 Sending request to update community comment report status. ReportId: {ReportId}, IsKeep: {IsKeep}, IsDelete: {IsDelete}",
                    dto.ReportId, dto.IsKeep, dto.IsDelete);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;

                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "Failed to deserialize ProblemDetails. Raw: {ErrorJson}", errorJson);
                        errorMessage = errorJson;
                    }

                    _logger.LogError(" Error from internal service for ReportId: {ReportId}. StatusCode: {StatusCode}, Error: {ErrorMessage}",
                        dto.ReportId, response.StatusCode, errorMessage);

                    throw new InvalidDataException(errorMessage);
                }
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    _logger.LogWarning(" Empty response from content service for ReportId: {ReportId}", dto.ReportId);
                    return "Empty response from content service";
                }

                try
                {
                    var message = JsonSerializer.Deserialize<string>(rawJson);
                    _logger.LogInformation("Successfully updated report status. ReportId: {ReportId}, Message: {Message}", dto.ReportId, message);
                    return message ?? "Unknown response from content service.";
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize success response. Raw JSON: {RawJson}", rawJson);
                    return $"Unexpected response format: {rawJson}";
                }
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, " Invalid data error while updating community comment report status. ReportId: {ReportId}", dto.ReportId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating community comment report status. ReportId: {ReportId}", dto.ReportId);
                throw new InvalidDataException($"Unexpected error: {ex.Message}", ex);
            }
        }




    }
}
