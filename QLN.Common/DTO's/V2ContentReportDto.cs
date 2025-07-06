using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2ContentReportArticleDto
    {
        public Guid Id { get; set; }
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
       
        public string? ReporterName { get; set; }
        public DateTime? ReportDate { get; set; }
        public bool IsActive { get; set; }
    }
    public class V2ReportCommunityPostDto
    {
        public Guid Id { get; set; }
        public Guid? PostId { get; set; }
        public string? Router { get; set; }
        public string? ReporterName { get; set; }
        public DateTime? ReportDate { get; set; }
        public bool? IsActive { get; set; }
    }
    public class V2NewsCommunitycommentsDto
    {
        public Guid Id { get; set; }
        public Guid? ArticleId { get; set; }
        public DateTime ComentDate { get; set; }
        public string? CommentText { get; set; }
        public string? AuthorName { get; set; }
        public bool IsActive { get; set; }
    }
    public class V2ReportsCommunitycommentsDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid CommentId { get; set; }
        public string ReporterName { get; set; }
        public DateTime ReportDate { get; set; }
        public bool IsActive { get; set; }
    }


    public class V2ContentReportArticleResponseDto
    {
        public Guid Id { get; set; }
        public Guid? PostId { get; set; }
        public string Post { get; set; }
        public Guid? CommentId { get; set; }
        public string? Reporter { get; set; }
        public DateTime? ReportDate { get; set; }

      
        public string? Comment { get; set; }
        public string? UserName { get; set; }
        public DateTime? CommentDate { get; set; }
    }

    public class V2UpdateReportStatusDto
    {
        public Guid ReportId { get; set; }
        public bool IsKeep { get; set; }
        public bool IsDelete { get; set; }
    }
    public class PaginatedReportsResponse
    {
        public List<V2ContentReportArticleResponseDto> Reports { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PerPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
    public class V2ContentReportCommunityCommentResponseDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid CommentId { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public DateTime? CommentDate { get; set; }
        public string? Title { get; set; }     
        public string? Comment { get; set; }
        public string? UserName { get; set; } 
    }

    public class GetAllReportQueryParams
    {
        public string SortOrder { get; set; } = "desc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? SearchTerm { get; set; }
    }
    public class V2UpdateCommunityCommentReportDto
    {
        public Guid ReportId { get; set; }
        public bool IsKeep { get; set; } = false;
        public bool IsDelete { get; set; } = false;
    }


}
