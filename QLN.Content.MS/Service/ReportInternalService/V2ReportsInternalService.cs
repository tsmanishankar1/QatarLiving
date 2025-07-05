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
        private readonly ILogger<V2InternalReportsService> _logger;

        public V2InternalReportsService(DaprClient dapr, ILogger<V2InternalReportsService> logger)
        {
            _dapr = dapr;
            _logger = logger; 
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
                    ComentDate = DateTime.Now,
                    AuthorName = userName,
                    CommentText = dto.CommentText,
                    IsActive = true,

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
                    ReportDate = DateTime.UtcNow,
                    IsActive = true,
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
      
  public async Task<List<V2ContentReportArticleResponseDto>> GetAllReports(
string sortOrder = "desc",
int pageNumber = 1,
int pageSize = 12,
string? searchTerm = null,
CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting GetAllReports with params: sortOrder={SortOrder}, pageNumber={PageNumber}, pageSize={PageSize}, searchTerm={SearchTerm}",
                    sortOrder, pageNumber, pageSize, searchTerm);

                // Validate parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 12;
                if (pageSize > 100) pageSize = 100; // Limit maximum page size

                // Fetch keys with error handling
                var reportKeys = await GetStateWithFallback<List<string>>(
                    ConstantValues.V2Content.ReportsIndexKey,
                    new List<string>(),
                    cancellationToken);

                var commentKeys = await GetStateWithFallback<List<string>>(
                    ConstantValues.V2Content.ReportsArticleCommentsIndexKey,
                    new List<string>(),
                    cancellationToken);

                _logger.LogInformation("Retrieved {ReportKeysCount} report keys and {CommentKeysCount} comment keys",
                    reportKeys.Count, commentKeys.Count);

                // Fetch reports in parallel
                var reportTasks = reportKeys.Select(async key =>
                {
                    try
                    {
                        return await _dapr.GetStateAsync<V2ContentReportArticleDto>(
                            ConstantValues.V2Content.ContentStoreName,
                            key,
                            cancellationToken: cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve report with key: {Key}", key);
                        return null;
                    }
                });

                var reportResults = await Task.WhenAll(reportTasks);
                var reports = reportResults
                    .Where(r => r != null && r.IsActive) 
                    .ToList();

                // Fetch comments in parallel
                var commentTasks = commentKeys.Select(async key =>
                {
                    try
                    {
                        return await _dapr.GetStateAsync<V2NewsCommunitycommentsDto>(
                            ConstantValues.V2Content.ContentStoreName,
                            key,
                            cancellationToken: cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve comment with key: {Key}", key);
                        return null;
                    }
                });

                var commentResults = await Task.WhenAll(commentTasks);
                var comments = commentResults.Where(c => c != null).ToList();

                // Fetch articles directly from the news index
                var newsKeys = await GetStateWithFallback<List<string>>(
                    ConstantValues.V2Content.NewsIndexKey,
                    new List<string>(),
                    cancellationToken);

                _logger.LogInformation("Fetched {Count} news keys from index", newsKeys.Count);

                // Initialize articles list
                var articles = new List<V2NewsArticleDTO>();

                // Only fetch articles if there are keys to fetch
                if (newsKeys != null && newsKeys.Any())
                {
                    var newsItems = await _dapr.GetBulkStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        newsKeys,
                        null,
                        cancellationToken: cancellationToken);

                    articles = newsItems
                        .Select(i =>
                        {
                            try
                            {
                                return JsonSerializer.Deserialize<V2NewsArticleDTO>(i.Value, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to deserialize news article with key: {Key}", i.Key);
                                return null;
                            }
                        })
                        .Where(dto => dto != null)
                        .ToList();
                }

                _logger.LogInformation("Deserialized {Count} news articles", articles.Count);

                // Create lookup dictionaries
                var commentLookup = comments.ToDictionary(c => c.Id, c => c);
                var articleLookup = articles.ToDictionary(a => a.Id, a => a);

                _logger.LogInformation("Processing {ReportsCount} reports with {CommentsCount} comments and {ArticlesCount} articles",
                    reports.Count, comments.Count, articles.Count);

                // Map to response DTOs with proper post title resolution
                var result = reports.Select(report =>
                {
                    commentLookup.TryGetValue(report.CommentId ?? Guid.Empty, out var comment);
                    string postTitle = null;
                    Guid? postId = null;

                    // Primary method: Use PostId from report to fetch article title
                    if (report.PostId != null)
                    {
                        postId = report.PostId;
                        if (articleLookup.TryGetValue((Guid)report.PostId, out var articleFromPostId))
                        {
                            postTitle = articleFromPostId?.Title;
                        }
                    }

                    // Secondary method: If no PostId in report, try to get from comment's ArticleId
                    if (string.IsNullOrEmpty(postTitle) && comment?.ArticleId != null)
                    {
                        postId = comment.ArticleId;
                        if (articleLookup.TryGetValue((Guid)comment.ArticleId, out var articleFromComment))
                        {
                            postTitle = articleFromComment?.Title;
                        }
                    }

                    // Log when post title is still null for debugging
                    if (string.IsNullOrEmpty(postTitle))
                    {
                        _logger.LogWarning("Could not find post title for report {ReportId} with PostId {PostId} and CommentId {CommentId}",
                            report.Id, report.PostId, report.CommentId);
                    }

                    return new V2ContentReportArticleResponseDto
                    {
                        Id = report.Id,
                        PostId = postId,
                        Post = postTitle,
                        CommentId = report.CommentId,
                        Reporter = report.ReporterName,
                        ReportDate = report.ReportDate,
                        Comment = comment?.CommentText,
                        UserName = comment?.AuthorName,
                        CommentDate = comment?.ComentDate
                    };
                }).ToList();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string lowerSearch = searchTerm.ToLower();
                    result = result.Where(r =>
                        (!string.IsNullOrEmpty(r.Post) && r.Post.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(r.Comment) && r.Comment.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(r.Reporter) && r.Reporter.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(r.UserName) && r.UserName.ToLower().Contains(lowerSearch))
                    ).ToList();
                }

                // Sort
                var sortedResult = sortOrder?.ToLower() switch
                {
                    "asc" => result.OrderBy(r => r.ReportDate),
                    "desc" => result.OrderByDescending(r => r.ReportDate),
                    _ => result.OrderByDescending(r => r.ReportDate)
                };

                // Apply pagination
                var pagedResult = sortedResult
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Returning {Count} reports after filtering and pagination", pagedResult.Count);

                return pagedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllReports service method");
                throw;
            }
        }

        // Helper method for state retrieval with fallback
        private async Task<T> GetStateWithFallback<T>(string key, T fallback, CancellationToken cancellationToken)
        {
            try
            {
                return await _dapr.GetStateAsync<T>(
                    ConstantValues.V2Content.ContentStoreName,
                    key,
                    cancellationToken: cancellationToken
                ) ?? fallback;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve state for key: {Key}, using fallback", key);
                return fallback;
            }
        }
        public async Task<string> UpdateReportStatus(V2UpdateReportStatusDto dto, CancellationToken cancellationToken = default)
        {
            string storeName = ConstantValues.V2Content.ContentStoreName;
            string reportIndexKey = ConstantValues.V2Content.ReportsIndexKey;
            string commentIndexKey = ConstantValues.V2Content.ReportsArticleCommentsIndexKey;

            try
            {
                Console.WriteLine("==> Begin UpdateReportStatus");
                Console.WriteLine($"IsKeep: {dto.IsKeep}, IsDelete: {dto.IsDelete}");

                if (dto.IsKeep && dto.IsDelete)
                    throw new InvalidDataException("Cannot set both IsKeep and IsDelete to true simultaneously.");
                if (!dto.IsKeep && !dto.IsDelete)
                    throw new InvalidDataException("Either IsKeep or IsDelete must be true.");

                var reportKeys = await _dapr.GetStateAsync<List<string>>(storeName, reportIndexKey) ?? new List<string>();
                var commentKeys = await _dapr.GetStateAsync<List<string>>(storeName, commentIndexKey) ?? new List<string>();

                Console.WriteLine($"Loaded {reportKeys.Count} report keys and {commentKeys.Count} comment keys");

                int updatedCount = 0;

                foreach (var reportKey in reportKeys)
                {
                    if (string.IsNullOrWhiteSpace(reportKey))
                    {
                        Console.WriteLine("Skipping null or empty report key");
                        continue;
                    }

                    Console.WriteLine($"Fetching report with key: {reportKey}");
                    var report = await _dapr.GetStateAsync<V2ContentReportArticleDto>(storeName, reportKey);

                    if (report == null)
                    {
                        Console.WriteLine($"Report not found for key: {reportKey}");
                        continue;
                    }

                    Console.WriteLine($"Report found: ID={report.Id}, IsActive={report.IsActive}, CommentId={report.CommentId}");

                    if (dto.IsKeep && report.IsActive)
                    {
                        report.IsActive = false;

                        try
                        {
                            Console.WriteLine($"Saving updated report (inactive): {reportKey}");
                            await _dapr.SaveStateAsync(storeName, reportKey, report);
                            updatedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error saving report with key {reportKey}: {ex.Message}");
                            throw new InvalidDataException($"Failed to save report state for key: {reportKey}", ex);
                        }
                    }

                    if (dto.IsDelete && report.CommentId.HasValue)
                    {
                        var commentIdStr = report.CommentId.Value.ToString();

                        if (!commentKeys.Contains(commentIdStr))
                        {
                            Console.WriteLine($"Comment ID {commentIdStr} not found in comment index");
                            continue;
                        }

                        Console.WriteLine($"Fetching comment with ID: {commentIdStr}");
                        var comment = await _dapr.GetStateAsync<V2NewsCommunitycommentsDto>(storeName, commentIdStr);

                        if (comment == null)
                        {
                            Console.WriteLine($"Comment not found: {commentIdStr}");
                            continue;
                        }

                        Console.WriteLine($"Comment found: ID={comment.Id}, IsActive={comment.IsActive}");

                        if (!comment.IsActive)
                        {
                            Console.WriteLine($"Comment {comment.Id} is already inactive");
                            continue;
                        }

                        comment.IsActive = false;

                        try
                        {
                            Console.WriteLine($"Saving updated comment (inactive): {commentIdStr}");
                            await _dapr.SaveStateAsync(storeName, commentIdStr, comment);
                            updatedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error saving comment with key {commentIdStr}: {ex.Message}");
                            throw new InvalidDataException($"Failed to save comment state for key: {commentIdStr}", ex);
                        }
                    }
                }

                Console.WriteLine($"✅ UpdateReportStatus finished. Updated entries: {updatedCount}");

                return updatedCount > 0
                    ? $"Successfully updated {updatedCount} entries."
                    : "No matching entries were found to update.";
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine($"❌ InvalidDataException: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                throw new InvalidDataException($"Unexpected error: {ex.Message}", ex);
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
