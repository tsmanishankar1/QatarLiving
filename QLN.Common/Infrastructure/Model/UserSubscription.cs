using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class UserSubscription
    {
        public Guid UserId { get; set; }
        public required Guid Id { get; set; }
        public required string DisplayName { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
