using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class BulkAdActionResponse
    {
        public int SuccessCount { get; set; }
        public List<long> FailedAdIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;        
    }
    public class BulkAdActionResponseitems
    {
        public ResultGroup Failed { get; set; } = new();
        public ResultGroup Succeeded { get; set; } = new();
    }

    public class ResultGroup
    {
        public int Count { get; set; }
        public List<long> Ids { get; set; } = new();
        public string? Reason { get; set; }
    }

}
