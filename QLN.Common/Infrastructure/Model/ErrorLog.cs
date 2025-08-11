using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class ErrorLog
    {
        public Guid Id { get; set; }
        public string Module { get; set; } = default!;
        public string ApiEndpoint { get; set; } = default!;
        public string ExceptionMessage { get; set; } = default!;
        public string? StackTrace { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
    }
}
