using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2ReportCommunityPost
    {
        public class CommunityPostWithReports
        {
            public Guid Id { get; set; }
            public string? Title { get; set; }
            public string? UserName { get; set; }
            public DateTime? DateCreated { get; set; }
            public List<CommunityPostReport> Reports { get; set; } = new();
        }

        public class CommunityPostReport
        {
            public string? ReporterName { get; set; }
            public DateTime? ReportDate { get; set; }
        }
        public class PaginatedCommunityPostResponse
        {
            public List<CommunityPostWithReports> Posts { get; set; } = new();
            public int TotalCount { get; set; }
            public int? Page { get; set; }
            public int? PerPage { get; set; }
        }
        public class V2ReportStatus
        {
            public Guid ReportId { get; set; }
            [DefaultValue(false)]
            public bool IsKeep { get; set; } = false;

            [DefaultValue(false)]
            public bool IsDelete { get; set; } = false;
        }
    }
}
