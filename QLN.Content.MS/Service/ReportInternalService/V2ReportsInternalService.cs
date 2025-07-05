using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text.Json;
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
                    ConstantValues.V2Content.ReportsIndexKey,
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
                var reportKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                var commentKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.ReportsArticleCommentsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                var reportTasks = reportKeys.Select(async key =>
                {
                    var report = await _dapr.GetStateAsync<V2ContentReportArticleDto>(
                        ConstantValues.V2Content.ContentStoreName,
                        key,
                        cancellationToken: cancellationToken
                    );
                    return report;
                });

                var reportResults = await Task.WhenAll(reportTasks);
                var reports = reportResults.Where(r => r != null).ToList();

                var commentTasks = commentKeys.Select(async key =>
                {
                    var comment = await _dapr.GetStateAsync<V2NewsCommunitycommentsDto>(
                        ConstantValues.V2Content.ContentStoreName,
                        key,
                        cancellationToken: cancellationToken
                    );
                    return comment;
                });

                var commentResults = await Task.WhenAll(commentTasks);
                var comments = commentResults.Where(c => c != null).ToList();

                // Fetch articles using the same pattern as GetAllNewsArticlesAsync
                var articleKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.NewsIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                var articleItems = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    articleKeys,
                    null,
                    cancellationToken: cancellationToken
                );

                var articles = articleItems
                    .Select(i => JsonSerializer.Deserialize<V2NewsArticleDTO>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(dto => dto != null)
                    .ToList();

                // Create lookup dictionaries
                var commentLookup = comments.ToDictionary(c => c.Id, c => c);
                var articleLookup = articles.ToDictionary(a => a.Id, a => a);

                var result = reports.Select(report =>
                {
                    commentLookup.TryGetValue(report.CommentId ?? Guid.Empty, out var comment);

                    string postTitle = null;
                    if (comment?.ArticleId != null)
                    {
                        // Match PostId (ArticleId from comment) with article Id to fetch title
                        articleLookup.TryGetValue((Guid)comment.ArticleId, out var article);
                        postTitle = article?.Title;
                    }

                    return new V2ContentReportArticleResponseDto
                    {
                        Id = report.Id,
                        PostId = comment?.ArticleId,
                        Post = postTitle, 
                        CommentId= report.CommentId,
                        Reporter = report.ReporterName,
                        ReportDate = report.ReportDate,
                        Comment = comment?.CommentText,
                        UserName = comment?.AuthorName,
                        CommentDate = comment?.ComentDate
                    };
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error occurred while getting all reports from state store");
                throw;
            }
        }
    }
}
