using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class AdStatusHistory
    {
        [Key]
        public Guid Id { get; set; }
        public long AdId { get; set; }
        public Vertical Vertical { get; set; }

        public SubVertical SubVertical { get; set; }

        public BulkActionEnum Action { get; set; }

        public string? Reason { get; set; }
        public string? Comments { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
