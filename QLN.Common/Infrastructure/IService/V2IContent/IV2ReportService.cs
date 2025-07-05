using QLN.Common.DTO_s;
using static QLN.Common.DTO_s.V2ReportCommunityPost;


namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2ReportsService
    {
        Task<string> CreateReport(string userName, V2ContentReportArticleDto dto, CancellationToken cancellationToken = default);
        Task<string> CreateCommunityReport(string userName, V2ReportCommunityPostDto dto, CancellationToken cancellationToken = default);
         Task<string> CreateCommunityCommentReport(string userName, V2ReportsCommunitycommentsDto dto, CancellationToken cancellationToken = default);
        Task<List<V2ContentReportArticleResponseDto>> GetAllReports(   string sortOrder = "desc",   int pageNumber = 1,   int pageSize = 12,   string? searchTerm = null,   CancellationToken cancellationToken = default);
        Task<string> CreateArticleComment(string userName, V2NewsCommunitycommentsDto dto, CancellationToken cancellationToken = default);
        Task<CommunityPostWithReports?> GetCommunityPostWithReport(Guid postId, CancellationToken ct);
        Task<List<CommunityPostWithReports>> GetAllCommunityPostsWithReports(CancellationToken ct);
        Task<PaginatedCommunityPostResponse> GetAllCommunityPostsWithPagination( int? pageNumber, int? perPage, string? searchTitle = null, string? sortBy = null, CancellationToken ct = default);
        Task<string> UpdateReportStatus(V2UpdateReportStatusDto dto, CancellationToken cancellationToken = default);
    }
}
