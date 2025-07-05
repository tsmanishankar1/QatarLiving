using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text;
using System.Text.Json;
using static QLN.Common.DTO_s.V2ReportCommunityPost;
using static QLN.Common.Infrastructure.Constants.ConstantValues;
namespace QLN.Content.MS.Service.ReportInternalService
{
    public class V2InternalReportsService : IV2ReportsService
    {
        private readonly DaprClient _dapr;

        public V2InternalReportsService(DaprClient dapr)
        {
            _dapr = dapr;
        }
        public async Task<string> CreateArticleComment(string userName, V2NewsCommunitycommentsDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                //ValidateReportRequest(dto);

                var id = Guid.NewGuid();
                var entity = new V2NewsCommunitycommentsDto
                {
                    Id = id,
                    ArticleId = dto.ArticleId,
                    ComentDate=DateTime.Now,
                    AuthorName=userName,
                    CommentText=dto.CommentText,
                   
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    entity,
                    cancellationToken: cancellationToken
                );

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsArticleCommentsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.ReportsArticleCommentsIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "Comments created successfully.";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating report", ex);
            }
        }
        public async Task<string> CreateReport(string userName, V2ContentReportArticleDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                //ValidateReportRequest(dto);

                var id = Guid.NewGuid();
                var entity = new V2ContentReportArticleDto
                {
                    Id = id,
                    PostId = dto.PostId,
                    CommentId = dto.CommentId,
                    ReporterName = userName,
                    ReportDate = DateTime.UtcNow
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    entity,
                    cancellationToken: cancellationToken
                );

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.ReportsIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "Report created successfully.";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating report", ex);
            }
        }
        public async Task<string> CreateCommunityCommentReport(string userName, V2ReportsCommunitycommentsDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                //ValidateReportRequest(dto);

                var id = Guid.NewGuid();
                var entity = new V2ReportsCommunitycommentsDto
                {
                    Id = id,
                    PostId = dto.PostId,
                    CommentId = dto.CommentId,
                    ReporterName =userName,
                    ReportDate = DateTime.UtcNow
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    entity,
                    cancellationToken: cancellationToken
                );

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsCommunityCommentsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.ReportsCommunityCommentsIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "Report created successfully.";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating report", ex);
            }
        }
        public async Task<string> CreateCommunityReport(string userName, V2ReportCommunityPostDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                //ValidateReportRequest(dto);

                var id = Guid.NewGuid();
                var entity = new V2ReportCommunityPostDto
                {
                    Id = id,
                    PostId = dto.PostId,
                    ReporterName = userName,
                    ReportDate = DateTime.UtcNow
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    entity,
                    cancellationToken: cancellationToken
                );

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsCommunityIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.ReportsCommunityIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "Community Report created successfully.";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating report", ex);
            }
        }
        public async Task<List<V2ContentReportArticleResponseDto>> GetAllReports(CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Get all reports
                var reports = await _dapr.InvokeMethodAsync<List<V2ContentReportArticleDto>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    "/api/v2/report/getAll",
                    cancellationToken
                ) ?? new List<V2ContentReportArticleDto>();
                var comments = await _dapr.InvokeMethodAsync<List<V2NewsCommunitycommentsDto>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    "/api/v2/comments/getAll", 
                    cancellationToken
                ) ?? new List<V2NewsCommunitycommentsDto>();

                // Step 3: Merge data
                var result = reports.Select(report =>
                {
                    var comment = comments.FirstOrDefault(c => c.ArticleId == report.PostId);

                    return new V2ContentReportArticleResponseDto
                    {
                        Id = report.Id,
                        PostId = report.PostId,
                        //CommentId = comment.CommentId,
                        ReporterName = report.ReporterName,
                        ReportDate = report.ReportDate,
                        CommentText = comment?.CommentText,
                        AuthorName = comment?.AuthorName,
                        CommentDate = comment?.ComentDate
                    };
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
               
                throw;
            }
        }


        public async Task<V2ContentReportArticleResponseDto?> GetReportById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetStateAsync<V2ContentReportArticleDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);

                if (result == null)
                    throw new KeyNotFoundException($"Report with id '{id}' was not found.");

                return new V2ContentReportArticleResponseDto
                {
                    Id = result.Id,
                    PostId = result.PostId,
                  //  UserName = result.UserName,
                    ReporterName = result.ReporterName,
                    ReportDate = result.ReportDate
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving report with ID: {id}", ex);
            }
        }

     
        public async Task<string> UpdateReport(string userId, V2ContentReportArticleDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Id == Guid.Empty)
                    throw new ArgumentException("Report ID is required for update.");

                var existing = await _dapr.GetStateAsync<V2ContentReportArticleDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Report with ID {dto.Id} not found.");

                var updated = new V2ContentReportArticleDto
                {
                    Id = dto.Id,
                    PostId = dto.PostId,
                  //  UserName = dto.UserName,
                    ReporterName = dto.ReporterName,
                    ReportDate = existing.ReportDate 
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    updated,
                    cancellationToken: cancellationToken);

                return "Report updated successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating report", ex);
            }
        }

        public async Task<string> DeleteReport(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _dapr.GetStateAsync<V2ContentReportArticleDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Report with ID '{id}' not found.");
                await _dapr.DeleteStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (keys.Contains(id.ToString()))
                {
                    keys.Remove(id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.ReportsIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "Report deleted successfully";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting report with ID: {id}", ex);
            }
        }
        public async Task<CommunityPostWithReports?> GetCommunityPostWithReport(Guid postId, CancellationToken ct)
        {
            try
            {
                var postKey = $"community-{postId}";

                var rawPostElement = await _dapr.GetStateAsync<JsonElement?>(V2Content.ContentStoreName, postKey, cancellationToken: ct);
                var rawReportsElement = await _dapr.GetStateAsync<JsonElement?>(V2Content.ContentStoreName, V2Content.ReportsCommunityIndexKey, cancellationToken: ct);
                if (rawPostElement == null || rawPostElement.Value.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }
                var post = JsonSerializer.Deserialize<V2CommunityPostDto>(rawPostElement.Value.GetRawText());
                if (post == null)
                {
                    return null;
                }
                List<CommunityPostReport> reports = new();
                if (rawReportsElement != null && rawReportsElement.Value.ValueKind != JsonValueKind.Null)
                {
                    var reportIds = JsonSerializer.Deserialize<List<Guid>>(rawReportsElement.Value.GetRawText());
                    if (reportIds != null)
                    {
                        foreach (var reportId in reportIds)
                        {
                            var reportKey = reportId.ToString();
                            var reportElement = await _dapr.GetStateAsync<V2ReportCommunityPostDto?>(V2Content.ContentStoreName, reportKey, cancellationToken: ct);

                            if (reportElement != null)
                            {
                                if (reportElement != null && reportElement.PostId == postId)
                                {
                                    reports.Add(new CommunityPostReport
                                    {
                                        ReporterName = reportElement.ReporterName,
                                        ReportDate = reportElement.ReportDate
                                    });
                                }
                            }
                        }
                    }
                }
                return new CommunityPostWithReports
                {
                    Id = post.Id,
                    Title = post.Title,
                    UserName = post.UserName,
                    DateCreated = post.DateCreated,
                    Reports = reports
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<List<CommunityPostWithReports>> GetAllCommunityPostsWithReports(CancellationToken ct)
        {
            try
            {
                var allPosts = new List<CommunityPostWithReports>();
                var allPostsElement = await _dapr.GetStateAsync<JsonElement?>(V2Content.ContentStoreName, "community-index", cancellationToken: ct);

                if (allPostsElement == null || allPostsElement.Value.ValueKind == JsonValueKind.Null)
                {
                    return allPosts;
                }
                List<Guid> postIds = new();
                var postKeys = JsonSerializer.Deserialize<List<string>>(allPostsElement.Value.GetRawText());
                if (postKeys != null)
                {
                    foreach (var key in postKeys)
                    {
                        if (key.StartsWith("community-"))
                        {
                            var guidPart = key.Substring("community-".Length);
                            if (Guid.TryParse(guidPart, out var guid))
                            {
                                postIds.Add(guid);
                            }
                        }
                    }
                }

                if (!postIds.Any())
                {
                    return allPosts;
                }
                var rawReportsElement = await _dapr.GetStateAsync<JsonElement?>("contentstatestore", V2Content.ReportsCommunityIndexKey, cancellationToken: ct);
                List<Guid> reportIds = new();

                if (rawReportsElement != null && rawReportsElement.Value.ValueKind != JsonValueKind.Null)
                {
                        var deserializedReportIds = JsonSerializer.Deserialize<List<Guid>>(rawReportsElement.Value.GetRawText());
                        if (deserializedReportIds != null)
                        {
                            reportIds = deserializedReportIds;
                        }
                }

                foreach (var postId in postIds)
                {
                    var postKey = $"community-{postId}";
                    var rawPostElement = await _dapr.GetStateAsync<JsonElement?>("contentstatestore", postKey, cancellationToken: ct);

                    if (rawPostElement == null || rawPostElement.Value.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }
                    var post = JsonSerializer.Deserialize<V2CommunityPostDto>(rawPostElement.Value.GetRawText());
                    if (post == null)
                    {
                        continue;
                    }
                    List<CommunityPostReport> reports = new();
                    if (reportIds.Any())
                    {
                        foreach (var reportId in reportIds)
                        {
                            var reportKey = reportId.ToString();
                            var reportElement = await _dapr.GetStateAsync<V2ReportCommunityPostDto?>(V2Content.ContentStoreName, reportKey, cancellationToken: ct);

                            if (reportElement != null && reportElement.PostId == postId)
                            {
                                reports.Add(new CommunityPostReport
                                {
                                    ReporterName = reportElement.ReporterName,
                                    ReportDate = reportElement.ReportDate
                                });
                            }
                        }
                    }
                    allPosts.Add(new CommunityPostWithReports
                    {
                        Id = post.Id,
                        Title = post.Title,
                        UserName = post.UserName,
                        DateCreated = post.DateCreated,
                        Reports = reports
                    });
                }
                return allPosts;
            }
            catch (Exception)
            {
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
                var allPosts = new List<CommunityPostWithReports>();
                var allPostsElement = await _dapr.GetStateAsync<JsonElement?>(V2Content.ContentStoreName, "community-index", cancellationToken: ct);

                if (allPostsElement == null || allPostsElement.Value.ValueKind == JsonValueKind.Null)
                {
                    return new PaginatedCommunityPostResponse();
                }

                var postKeys = JsonSerializer.Deserialize<List<string>>(allPostsElement.Value.GetRawText());
                var postIds = postKeys?
                    .Where(k => k.StartsWith("community-") && Guid.TryParse(k["community-".Length..], out _))
                    .Select(k => Guid.Parse(k["community-".Length..]))
                    .ToList() ?? new List<Guid>();

                if (!postIds.Any())
                {
                    return new PaginatedCommunityPostResponse();
                }

                var rawReportsElement = await _dapr.GetStateAsync<JsonElement?>("contentstatestore", V2Content.ReportsCommunityIndexKey, cancellationToken: ct);
                var reportIds = rawReportsElement?.ValueKind != JsonValueKind.Null
                    ? JsonSerializer.Deserialize<List<Guid>>(rawReportsElement.Value.GetRawText()) ?? new List<Guid>()
                    : new List<Guid>();

                foreach (var postId in postIds)
                {
                    var postKey = $"community-{postId}";
                    var rawPostElement = await _dapr.GetStateAsync<JsonElement?>("contentstatestore", postKey, cancellationToken: ct);
                    if (rawPostElement is null || rawPostElement.Value.ValueKind == JsonValueKind.Null) continue;

                    var post = JsonSerializer.Deserialize<V2CommunityPostDto>(rawPostElement.Value.GetRawText());
                    if (post is null) continue;

                    var reports = new List<CommunityPostReport>();
                    foreach (var reportId in reportIds)
                    {
                        var reportKey = reportId.ToString();
                        var reportElement = await _dapr.GetStateAsync<V2ReportCommunityPostDto?>(V2Content.ContentStoreName, reportKey, cancellationToken: ct);
                        if (reportElement != null && reportElement.PostId == postId)
                        {
                            reports.Add(new CommunityPostReport
                            {
                                ReporterName = reportElement.ReporterName,
                                ReportDate = reportElement.ReportDate
                            });
                        }
                    }

                    allPosts.Add(new CommunityPostWithReports
                    {
                        Id = post.Id,
                        Title = post.Title,
                        UserName = post.UserName,
                        DateCreated = post.DateCreated,
                        Reports = reports
                    });
                }

                if (!string.IsNullOrWhiteSpace(searchTitle))
                {
                    allPosts = allPosts
                        .Where(p => p.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var sortKey = sortBy?.ToLowerInvariant();
                allPosts = sortKey switch
                {
                    "title" => allPosts.OrderBy(p => p.Title).ToList(),
                    "title_desc" => allPosts.OrderByDescending(p => p.Title).ToList(),
                    "username" => allPosts.OrderBy(p => p.UserName).ToList(),
                    "username_desc" => allPosts.OrderByDescending(p => p.UserName).ToList(),
                    "date" => allPosts.OrderBy(p => p.DateCreated).ToList(),
                    "date_desc" => allPosts.OrderByDescending(p => p.DateCreated).ToList(),
                    "reports" => allPosts.OrderBy(p => p.Reports.Count).ToList(),
                    "reports_desc" => allPosts.OrderByDescending(p => p.Reports.Count).ToList(),
                    "asc" => allPosts.OrderBy(p => p.Reports.Any() ? p.Reports.Max(r => r.ReportDate) : DateTime.MinValue).ToList(),
                    "desc" => allPosts.OrderByDescending(p => p.Reports.Any() ? p.Reports.Max(r => r.ReportDate) : DateTime.MinValue).ToList(),
                    _ => allPosts.OrderByDescending(p => p.DateCreated).ToList()
                };

                var totalCount = allPosts.Count;
                var currentPage = pageNumber ?? 1;
                var pageSize = perPage ?? 12;
                var paginatedPosts = allPosts
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PaginatedCommunityPostResponse
                {
                    Posts = paginatedPosts,
                    TotalCount = totalCount,
                    Page = currentPage,
                    PerPage = pageSize,
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
