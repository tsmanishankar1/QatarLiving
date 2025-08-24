using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Classifieds
{
    public class ClassifiedsPayToPromote
    {
        public long AdId { get; set; }
        public Guid AddonId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical SubVertical { get; set; }
    }

    public sealed class P2PromoteResponseDto
    {
        public SubVertical SubVertical { get; set; }
        public long AdId { get; set; }
        public bool Promoted { get; set; }
        public Guid AddonId { get; set; }
    }
}
