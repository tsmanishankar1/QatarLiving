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
        public List<Guid> FailedAdIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;        
    }    
}
