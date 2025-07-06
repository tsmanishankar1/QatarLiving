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
            public Guid PostId { get; set; }
            public string? Post { get; set; }
            public string? UserName { get; set; }
            public DateTime? PostDate { get; set; }
            public string? Reporter { get; set; }
            public DateTime? ReportDate { get; set; }
            public string? Router { get; set; }
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
