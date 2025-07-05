using QLN.Common.DTO_s;


namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2ReportsService
    {
        Task<string> CreateReport(string userName, V2ContentReportArticleDto dto, CancellationToken cancellationToken = default);
        Task<string> CreateCommunityReport(string userName, V2ReportCommunityPostDto dto, CancellationToken cancellationToken = default);
         Task<string> CreateCommunityCommentReport(string userName, V2ReportsCommunitycommentsDto dto, CancellationToken cancellationToken = default);
        Task<List<V2ContentReportArticleResponseDto>> GetAllReports(CancellationToken cancellationToken = default);
        Task<string> CreateArticleComment(string userName, V2NewsCommunitycommentsDto dto, CancellationToken cancellationToken = default);
    }
}
