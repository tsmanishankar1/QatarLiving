//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace QLN.Common.DTO_s.AuditLog
//{
//    public class AuditLog
//    {
//        public Guid Id { get; set; }
//        public string Module { get; set; } = null!;
//        public string HttpMethod { get; set; } = null!;
//        public string ApiEndpoint { get; set; } = null!;
//        public string SuccessMessage { get; set; } = null!;
//        public string CreatedBy { get; set; }
//        public string? Payload { get; set; }
//        public DateTime CreatedUtc { get; set; }
//    }
//    public class UpdateAuditLog
//    {
//        public Guid Id { get; set; }
//        public string Module { get; set; } = null!;
//        public string HttpMethod { get; set; } = null!;
//        public string ApiEndpoint { get; set; } = null!;
//        public string SuccessMessage { get; set; } = null!;
//        public string UpdatedBy { get; set; }
//        public string? Payload { get; set; }
//        public DateTime UpdatedUtc { get; set; }
//    }
//}
