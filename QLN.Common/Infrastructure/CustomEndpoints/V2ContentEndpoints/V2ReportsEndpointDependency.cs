using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2ReportsEndpointDependency
    {
        public static RouteGroupBuilder MapReportsEndpoints(this RouteGroupBuilder group)
        {
            group.MapCreateReportEndpoints()
                .MapCreateCommunityPostReportEndpoints()
                .MapCreateCommunityCommentsEndpoints()
                .MapGetAllReportsEndpoints()
                .MapGetReportCommunityPost()
                .MapGetAllCommunityPostReports()
                .MapGetAllCommunityPostsWithPagination()
                .MapUpdateArticleCommentStatusEndpoints()
                  .MapGetAllCommunityCommentsReports()
                 .MapCreateNewsCommentEndpoints();
            return group;
        }
    }
}