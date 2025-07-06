using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
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
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));

                if (dto.PostId == Guid.Empty || dto.CommentId == Guid.Empty)
                    throw new ArgumentException("PostId and CommentId are required.");

                var id = Guid.NewGuid();

                var entity = new V2ReportsCommunitycommentsDto
                {
                    Id = id,
                    PostId = dto.PostId,
                    CommentId = dto.CommentId,
                    ReporterName = userName,
                    ReportDate = DateTime.UtcNow,
                    IsActive=true,
                };

                _logger.LogInformation("Saving report: ID={Id}, PostId={PostId}, CommentId={CommentId}, Reporter={Reporter}",
                    entity.Id, entity.PostId, entity.CommentId, entity.ReporterName);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community comment report");
                throw;
            }
        }
        public async Task<string> CreateCommunityReport(string userName, V2ReportCommunityPostDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                var entity = new V2ReportCommunityPostDto
                {
                    Id = id,
                    PostId = dto.PostId,
                    Router = dto.Router,
                    ReporterName = userName,
                    IsActive = true,
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

                return "Community Post Report created successfully.";
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
    
        public async Task<List<V2ContentReportArticleResponseDto>> GetAllReports(string sortOrder = "desc",int pageNumber = 1,int pageSize = 12,string? searchTerm = null,CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting GetAllReports with params: sortOrder={SortOrder}, pageNumber={PageNumber}, pageSize={PageSize}, searchTerm={SearchTerm}",
                    sortOrder, pageNumber, pageSize, searchTerm);
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 12;
                if (pageSize > 100) pageSize = 100; 

            
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

               
                var newsKeys = await GetStateWithFallback<List<string>>(
                    ConstantValues.V2Content.NewsIndexKey,
                    new List<string>(),
                    cancellationToken);

                _logger.LogInformation("Fetched {Count} news keys from index", newsKeys.Count);

               
                var articles = new List<V2NewsArticleDTO>();

               
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

               
                var commentLookup = comments.ToDictionary(c => c.Id, c => c);
                var articleLookup = articles.ToDictionary(a => a.Id, a => a);

                _logger.LogInformation("Processing {ReportsCount} reports with {CommentsCount} comments and {ArticlesCount} articles",
                    reports.Count, comments.Count, articles.Count);

               
                var result = reports.Select(report =>
                {
                    commentLookup.TryGetValue(report.CommentId ?? Guid.Empty, out var comment);
                    string postTitle = null;
                    Guid? postId = null;

                   
                    if (report.PostId != null)
                    {
                        postId = report.PostId;
                        if (articleLookup.TryGetValue((Guid)report.PostId, out var articleFromPostId))
                        {
                            postTitle = articleFromPostId?.Title;
                        }
                    }

                   
                    if (string.IsNullOrEmpty(postTitle) && comment?.ArticleId != null)
                    {
                        postId = comment.ArticleId;
                        if (articleLookup.TryGetValue((Guid)comment.ArticleId, out var articleFromComment))
                        {
                            postTitle = articleFromComment?.Title;
                        }
                    }

                  
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

               
                var sortedResult = sortOrder?.ToLower() switch
                {
                    "asc" => result.OrderBy(r => r.ReportDate),
                    "desc" => result.OrderByDescending(r => r.ReportDate),
                    _ => result.OrderByDescending(r => r.ReportDate)
                };

              
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
                    if (dto.IsDelete)
                    {
                       
                        if (report.IsActive)
                        {
                            report.IsActive = false;

                            try
                            {
                                Console.WriteLine($"Saving updated report (inactive - delete): {reportKey}");
                                await _dapr.SaveStateAsync(storeName, reportKey, report);
                                updatedCount++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Error saving report with key {reportKey}: {ex.Message}");
                                throw new InvalidDataException($"Failed to save report state for key: {reportKey}", ex);
                            }
                        }
                        if (report.CommentId.HasValue)
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

                            if (comment.IsActive)
                            {
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
                            else
                            {
                                Console.WriteLine($"Comment {comment.Id} is already inactive");
                            }
                        }
                    }
                }

                Console.WriteLine("UpdateReportStatus finished. Updated entries: {updatedCount}");

                return updatedCount > 0
                    ? $"Successfully updated {updatedCount} entries."
                    : "No matching entries were found to update.";
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine("❌ InvalidDataException: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Unexpected error: {ex.Message}");
                throw new InvalidDataException($"Unexpected error: {ex.Message}", ex);
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
                var allPostsElement = await _dapr.GetStateAsync<JsonElement?>(
                    V2Content.ContentStoreName,
                    "community-index",
                    cancellationToken: ct);

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

                var rawReportsElement = await _dapr.GetStateAsync<JsonElement?>(
                    "contentstatestore",
                    V2Content.ReportsCommunityIndexKey,
                    cancellationToken: ct);

                var reportIds = rawReportsElement?.ValueKind != JsonValueKind.Null
                    ? JsonSerializer.Deserialize<List<Guid>>(rawReportsElement.Value.GetRawText()) ?? new List<Guid>()
                    : new List<Guid>();

                foreach (var postId in postIds)
                {
                    var postKey = $"community-{postId}";
                    var rawPostElement = await _dapr.GetStateAsync<JsonElement?>("contentstatestore", postKey, cancellationToken: ct);

                    if (rawPostElement is null || rawPostElement.Value.ValueKind == JsonValueKind.Null)
                        continue;

                    var post = JsonSerializer.Deserialize<V2CommunityPostDto>(rawPostElement.Value.GetRawText());

                    if (post is null || !post.IsActive)
                        continue;

                    foreach (var reportId in reportIds)
                    {
                        var reportKey = reportId.ToString();
                        var reportElement = await _dapr.GetStateAsync<V2ReportCommunityPostDto?>(
                            V2Content.ContentStoreName,
                            reportKey,
                            cancellationToken: ct);

                        if (reportElement != null &&
                            reportElement.PostId == postId &&
                            reportElement.IsActive == true)
                        {
                            allPosts.Add(new CommunityPostWithReports
                            {
                                Id = reportId,
                                PostId = post.Id,
                                Post = post.Title,
                                UserName = post.UserName,
                                PostDate = post.DateCreated,
                                Reporter = reportElement.ReporterName,
                                ReportDate = reportElement.ReportDate,
                                Router = reportElement.Router
                            });
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(searchTitle))
                {
                    allPosts = allPosts
                        .Where(p => p.Post != null && p.Post.Contains(searchTitle, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var sortKey = sortBy?.ToLowerInvariant();
                allPosts = sortKey switch
                {
                    "title" => allPosts.OrderBy(p => p.Post).ToList(),
                    "title_desc" => allPosts.OrderByDescending(p => p.Post).ToList(),
                    "username" => allPosts.OrderBy(p => p.UserName).ToList(),
                    "username_desc" => allPosts.OrderByDescending(p => p.UserName).ToList(),
                    "date" => allPosts.OrderBy(p => p.PostDate).ToList(),
                    "date_desc" => allPosts.OrderByDescending(p => p.PostDate).ToList(),
                    "reportdate" => allPosts.OrderBy(p => p.ReportDate).ToList(),
                    "reportdate_desc" => allPosts.OrderByDescending(p => p.ReportDate).ToList(),
                    _ => allPosts.OrderByDescending(p => p.PostDate).ToList()
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
                    PerPage = pageSize
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<string> UpdateCommunityPostReportStatus(V2ReportStatus dto, CancellationToken cancellationToken = default)
        {
            string storeName = ConstantValues.V2Content.ContentStoreName;
            string postReportIndexKey = ConstantValues.V2Content.ReportsCommunityIndexKey;
            string communityPostIndexKey = "community-index";
            try
            {
                if (dto.IsKeep && dto.IsDelete)
                    throw new InvalidDataException("Cannot set both IsKeep and IsDelete to true simultaneously.");
                if (!dto.IsKeep && !dto.IsDelete)
                    throw new InvalidDataException("Either IsKeep or IsDelete must be true.");
                var reportKeys = await _dapr.GetStateAsync<List<string>>(storeName, postReportIndexKey) ?? new List<string>();
                var postKeys = await _dapr.GetStateAsync<List<string>>(storeName, communityPostIndexKey) ?? new List<string>();

                int updatedCount = 0;
                foreach (var reportKey in reportKeys)
                {
                    if (string.IsNullOrWhiteSpace(reportKey))
                    {
                        Console.WriteLine("Skipping null or empty report key");
                        continue;
                    }
                    var report = await _dapr.GetStateAsync<V2ReportCommunityPostDto>(storeName, reportKey);

                    if (report == null)
                    {
                        continue;
                    }
                    if (report.PostId != dto.PostId)
                    {
                        Console.WriteLine($"Skipping report for different post: {report.PostId}");
                        continue;
                    }
                    if (dto.IsKeep && report.IsActive == true)
                    {
                        report.IsActive = false;

                        try
                        {
                            Console.WriteLine($"Saving updated report (kept): {reportKey}");
                            await _dapr.SaveStateAsync(storeName, reportKey, report);
                            updatedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error saving report with key {reportKey}: {ex.Message}");
                            throw new InvalidDataException($"Failed to save report state for key: {reportKey}", ex);
                        }
                    }
                    if (dto.IsDelete && report.IsActive == true)
                    {
                        report.IsActive = false;

                        try
                        {
                            await _dapr.SaveStateAsync(storeName, reportKey, report);
                            updatedCount++;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidDataException($"Failed to save report state for key: {reportKey}", ex);
                        }
                    }
                }

                if (dto.IsDelete)
                {
                    var postIdStr = $"community-{dto.PostId}";

                    if (postKeys.Contains(postIdStr))
                    {
                        var post = await _dapr.GetStateAsync<V2CommunityPostDto>(storeName, postIdStr);

                        if (post != null && post.IsActive)
                        {
                            post.IsActive = false;
                            try
                            {
                                await _dapr.SaveStateAsync(storeName, postIdStr, post);
                                updatedCount++;
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidDataException($"Failed to save post state for key: {postIdStr}", ex);
                            }
                        }
                    }
                }
                string actionType = dto.IsKeep ? "kept" : "deleted";
                return updatedCount > 0
                    ? $"Successfully {actionType} post and updated {updatedCount} entries."
                    : $"No matching entries were found to update for post {dto.PostId}."; 
            }
            catch (InvalidDataException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Unexpected error: {ex.Message}", ex);
            }
        }

        public async Task<List<V2ContentReportCommunityCommentResponseDto>> GetAllCommunityCommentReports(string sortOrder = "desc",int pageNumber = 1,int pageSize = 12,string? searchTerm = null,CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting GetAllCommunityCommentReports with sortOrder={SortOrder}, pageNumber={PageNumber}, pageSize={PageSize}, searchTerm={SearchTerm}",
                    sortOrder, pageNumber, pageSize, searchTerm);

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 12;
                if (pageSize > 100) pageSize = 100;

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsCommunityCommentsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                _logger.LogInformation("Found {Count} comment report keys", keys.Count);

                var reportTasks = keys.Select(key => _dapr.GetStateAsync<V2ReportsCommunitycommentsDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    key,
                    cancellationToken: cancellationToken
                ));

                var reportEntities = await Task.WhenAll(reportTasks);

               
                var reports = reportEntities
                    .Where(e => e != null && e.IsActive)
                    .ToList();

                var responseDtos = new List<V2ContentReportCommunityCommentResponseDto>();

                foreach (var report in reports)
                {
                    string? commentContent = null;
                    string? postTitle = null;
                    DateTime? commentedAt = null;
                    string? userName = null;

                    var postKey = $"communitypost-{report.PostId}";
                    var post = await _dapr.GetStateAsync<V2CommunityPostDto>(
                        ConstantValues.V2Content.ContentStoreName,
                        postKey,
                        cancellationToken: cancellationToken
                    );

                    if (post != null)
                    {
                        postTitle = post.Title;
                    }

                    var commentKey = $"comment-{report.PostId}-{report.CommentId}";
                    var comment = await _dapr.GetStateAsync<CommunityCommentDto>(
                        ConstantValues.V2Content.ContentStoreName,
                        commentKey,
                        cancellationToken: cancellationToken
                    );

                    if (comment != null)
                    {
                        commentContent = comment.Content;
                        commentedAt = comment.CommentedAt;
                        userName = comment.UserName;
                    }
                    else
                    {
                        var altCommentKey = $"comment-{report.CommentId}";
                        var altComment = await _dapr.GetStateAsync<CommunityCommentDto>(
                            ConstantValues.V2Content.ContentStoreName,
                            altCommentKey,
                            cancellationToken: cancellationToken
                        );

                        if (altComment != null)
                        {
                            commentContent = altComment.Content;
                            commentedAt = altComment.CommentedAt;
                            userName = altComment.UserName;
                        }
                    }

                    responseDtos.Add(new V2ContentReportCommunityCommentResponseDto
                    {
                        Id = report.Id,
                        PostId = report.PostId,
                        CommentId = report.CommentId,
                        ReporterName = report.ReporterName,
                        ReportDate = report.ReportDate,
                        Title = postTitle,
                        Comment = commentContent,
                        CommentDate = commentedAt,
                        UserName = userName
                    });
                }
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string search = searchTerm.ToLower();
                    responseDtos = responseDtos.Where(r =>
                        (!string.IsNullOrEmpty(r.ReporterName) && r.ReporterName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(r.Title) && r.Title.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(r.Comment) && r.Comment.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(r.UserName) && r.UserName.ToLower().Contains(search))
                    ).ToList();
                }
                responseDtos = sortOrder.ToLower() switch
                {
                    "asc" => responseDtos.OrderBy(r => r.ReportDate).ToList(),
                    _ => responseDtos.OrderByDescending(r => r.ReportDate).ToList()
                };

          
                responseDtos = responseDtos
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Returning {Count} enriched community comment reports", responseDtos.Count);

                return responseDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllCommunityCommentReports with post/comment enrichment");
                throw;
            }
        }
        public async Task<string> UpdateCommunityCommentReportStatus(V2UpdateCommunityCommentReportDto dto, CancellationToken cancellationToken = default)
        {
            string storeName = ConstantValues.V2Content.ContentStoreName;

            try
            {
                Console.WriteLine("==> Begin UpdateCommunityCommentReportStatus");
                Console.WriteLine($"ReportId: {dto.ReportId}, IsKeep: {dto.IsKeep}, IsDelete: {dto.IsDelete}");
                if (dto.ReportId == Guid.Empty)
                    throw new InvalidDataException("ReportId is required.");

                if (dto.IsKeep && dto.IsDelete)
                    throw new InvalidDataException("Cannot set both IsKeep and IsDelete to true simultaneously.");

                if (!dto.IsKeep && !dto.IsDelete)
                    throw new InvalidDataException("Either IsKeep or IsDelete must be true.");

                var reportKey = dto.ReportId.ToString();
                Console.WriteLine($"Fetching community comment report with ID: {reportKey} from store: {storeName}");

                var report = await _dapr.GetStateAsync<V2ReportsCommunitycommentsDto>(storeName, reportKey, cancellationToken: cancellationToken);

                if (report == null)
                {
                    Console.WriteLine($"❌ Report not found in state store. Key: {reportKey}");
                    throw new InvalidDataException($"Report with ID {dto.ReportId} not found.");
                }

                Console.WriteLine($"✅ Report found. ID={report.Id}, IsActive={report.IsActive}, CommentId={report.CommentId}, PostId={report.PostId}");

                int updatedCount = 0;

                if (dto.IsKeep && report.IsActive)
                {
                    report.IsActive = false;

                    try
                    {
                        await _dapr.SaveStateAsync(storeName, reportKey, report, cancellationToken: cancellationToken);
                        updatedCount++;
                        Console.WriteLine($"✅ Report {dto.ReportId} marked as inactive (kept).");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error saving report with ID {dto.ReportId}: {ex.Message}");
                        throw new InvalidDataException($"Failed to save report with ID {dto.ReportId}", ex);
                    }
                }

                if (dto.IsDelete)
                {
                    if (report.CommentId == Guid.Empty || report.PostId == Guid.Empty)
                    {
                        Console.WriteLine($"❌ Invalid CommentId or PostId in report {dto.ReportId}");
                        throw new InvalidDataException($"Invalid CommentId or PostId in report {dto.ReportId}");
                    }

                    var commentKey = $"comment-{report.PostId}-{report.CommentId}";
                    Console.WriteLine($"Fetching community comment with key: {commentKey}");

                    var comment = await _dapr.GetStateAsync<CommunityCommentDto>(storeName, commentKey, cancellationToken: cancellationToken);

                    if (comment == null)
                    {
                        Console.WriteLine($"❌ Comment not found in state store. Key: {commentKey}");
                        throw new InvalidDataException($"Comment not found for key: {commentKey}");
                    }

                    Console.WriteLine("Comment found. ID={comment.CommentId}, IsActive={comment.IsActive}");

                    if (comment.IsActive)
                    {
                        comment.IsActive = false;
                        try
                        {
                            await _dapr.SaveStateAsync(storeName, commentKey, comment, cancellationToken: cancellationToken);
                            updatedCount++;
                            Console.WriteLine(" Comment {comment.CommentId} marked as inactive.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(" Error saving comment with key {commentKey}: {ex.Message}");
                            throw new InvalidDataException($"Failed to save comment with key {commentKey}", ex);
                        }
                    }

                    if (report.IsActive)
                    {
                        report.IsActive = false;
                        try
                        {
                            await _dapr.SaveStateAsync(storeName, reportKey, report, cancellationToken: cancellationToken);
                            updatedCount++;
                            Console.WriteLine(" Report {dto.ReportId} marked as inactive (deleted).");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(" Error saving report with ID {dto.ReportId}: {ex.Message}");
                            throw new InvalidDataException($"Failed to update report with ID {dto.ReportId}", ex);
                        }
                    }
                }

                Console.WriteLine("Update complete. Total updated entries: {updatedCount}");

                return updatedCount > 0
                    ? $"Successfully updated {updatedCount} entries for report ID {dto.ReportId}."
                    : $"No updates needed for report ID {dto.ReportId}.";
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine(" InvalidDataException: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Unexpected error: {ex.Message}");
                throw new InvalidDataException($"Unexpected error occurred while updating report ID {dto.ReportId}: {ex.Message}", ex);
            }
        }

    }
}
