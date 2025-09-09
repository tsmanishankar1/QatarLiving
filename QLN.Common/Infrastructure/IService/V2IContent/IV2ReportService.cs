using QLN.Common.DTO_s;
using static QLN.Common.DTO_s.V2ReportCommunityPost;


namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2ReportsService
    {
        Task<string> CreateReport(string userName, V2ContentReportArticleDto dto, CancellationToken cancellationToken = default);
        Task<string> CreateCommunityReport(string userName, V2ReportCommunityPostDto dto, CancellationToken cancellationToken = default);
         Task<string> CreateCommunityCommentReport(string userName, V2ReportsCommunitycommentsDto dto, CancellationToken cancellationToken = default);
        Task<PagedResult<V2ContentReportArticleResponseDto>> GetAllReports(string sortOrder = "desc",int pageNumber = 1,int pageSize = 12,string? searchTerm = null,CancellationToken cancellationToken = default);
        Task<string> CreateArticleComment(string userName, V2NewsCommunitycommentsDto dto, CancellationToken cancellationToken = default);
        Task<PaginatedCommunityPostResponse> GetAllCommunityPostsWithPagination( int? pageNumber, int? pageSize, string? searchTerm = null, string? sortOrder = null, CancellationToken ct = default);
        Task<string> UpdateReportStatus(V2UpdateReportStatusDto dto, CancellationToken cancellationToken = default);
        Task<string> UpdateCommunityPostReportStatus(V2ReportStatus dto, CancellationToken cancellationToken = default);
        Task<PagedResult<V2ContentReportCommunityCommentResponseDto>> GetAllCommunityCommentReports(
           string sortOrder = "desc",
           int pageNumber = 1,
           int pageSize = 12,
           string? searchTerm = null,
           CancellationToken cancellationToken = default);
        Task<string> UpdateCommunityCommentReportStatus(V2UpdateCommunityCommentReportDto dto, CancellationToken cancellationToken = default);
    }
}
