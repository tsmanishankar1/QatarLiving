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
                        CommentId = comment.CommentId,
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
    }
}
