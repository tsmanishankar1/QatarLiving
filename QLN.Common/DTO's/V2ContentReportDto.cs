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
    }
    public class V2ReportCommunityPostDto
    {
        public Guid Id { get; set; }
        public Guid? PostId { get; set; }
        public string? ReporterName { get; set; }
        public DateTime? ReportDate { get; set; }
    }
    public class V2NewsCommunitycommentsDto
    {
        public Guid Id { get; set; }
        public Guid? ArticleId { get; set; }
        public DateTime ComentDate { get; set; }
        public  string? CommentText{get;set;}
        public string?AuthorName { get; set; }
    }
    public class V2ReportsCommunitycommentsDto
    {
        public Guid Id { get; set; }
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public string? ReporterName { get; set; }
        public DateTime? ReportDate { get; set; }
    }

    public class V2ContentReportArticleResponseDto
    {
        public Guid Id { get; set; }
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public string? ReporterName { get; set; }
        public DateTime? ReportDate { get; set; }

      
        public string? CommentText { get; set; }
        public string? AuthorName { get; set; }
        public DateTime? CommentDate { get; set; }
    }

}
