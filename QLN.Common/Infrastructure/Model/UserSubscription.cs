using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class UserSubscription
    {
        public Guid Id { get; set; }
        public required string DisplayName { get; set; }
    }
}
