using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class Comment
    {
        public long Id { get; set; }
        public long? AdId { get; set; }
        public Guid? CompanyId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public string Action { get; set; }
        public string? Reason { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedUserId { get; set; }
        public string CreatedUserName { get; set; } 
        public string? UpdatedUserId { get; set; }
        public string? UpdatedUserName { get; set; }
    }
}
