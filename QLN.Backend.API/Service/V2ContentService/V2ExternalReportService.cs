using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
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


        public async Task<List<V2ContentReportArticleResponseDto>> GetAllReports(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<V2ContentReportArticleResponseDto>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    "/api/v2/report/getAll",
                    cancellationToken
                ) ?? new List<V2ContentReportArticleResponseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all reports.");
                throw;
            }
        }

        public async Task<V2ContentReportArticleResponseDto?> GetReportById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/report/getById/{id}";

                return await _dapr.InvokeMethodAsync<V2ContentReportArticleResponseDto>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report for Id : {Id}", id);
                throw;
            }
        }



        public async Task<string> UpdateReport(string userId, V2ContentReportArticleDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/report/updateByUserId";

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

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report");
                throw;
            }
        }

        public async Task<string> DeleteReport(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/report/delete/{id}";

                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken
                ) ?? "Report deleted successfully";
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Report with ID {id} not found.", id);
                return "Report not found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report with Id {id}", id);
                throw;
            }
        }
        public async Task<CommunityPostWithReports?> GetCommunityPostWithReport(Guid postId, CancellationToken ct)
        {
            try
            {
                // Fix: Use correct parameter name that matches the endpoint
                var url = $"/api/v2/report/getpostwithreports?postId={postId}";
                return await _dapr.InvokeMethodAsync<CommunityPostWithReports>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    ct);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving community post with reports for PostId : {PostId}", postId);
                throw;
            }
        }
        public async Task<List<CommunityPostWithReports>> GetAllCommunityPostsWithReports(CancellationToken ct)
        {
            try
            {
                var url = $"/api/v2/report/getallcommunitypostwithreports";
                return await _dapr.InvokeMethodAsync<List<CommunityPostWithReports>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all community posts with reports.");
                throw;
            }
        }
        public async Task<PaginatedCommunityPostResponse> GetAllCommunityPostsWithPagination(
            int? pageNumber,
            int? perPage,
            string? searchTitle = null,
            string? sortBy = null,
            CancellationToken ct = default)
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
    }
}
