using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.AuditLog
{
    public class AuditLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Action { get; set; } 
        public string Entity { get; set; } 
        public string EntityId { get; set; } 
        public string PerformedBy { get; set; }
        public string UserName { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Details { get; set; } 
    }
    public class AuditEntry
    {
        public string Id { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string Entity { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string PerformedBy { get; set; } = default!;
        public DateTime Timestamp { get; set; }
        public string Data { get; set; } = default!;
    }

}
